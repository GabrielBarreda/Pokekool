using MediatR;
using Microsoft.Extensions.Logging;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Repositories;

namespace Pokekool.Application.Pokemon.Commands.SyncCardTranslations;

/// <summary>
/// Alimente <c>card_translations</c> à partir de TCGdex, set par set. Symétrique de
/// SyncPokemonTcgCommandHandler mais pour les noms traduits (jamais les données référentielles
/// — voir ADR-004, mémoire Obsidian du projet). Skip par carte (pas par set) : une carte déjà
/// traduite dans la langue demandée n'est pas retraitée, sauf ForceRefresh.
/// </summary>
public sealed class SyncCardTranslationsCommandHandler
    : IRequestHandler<SyncCardTranslationsCommand, SyncCardTranslationsResultDto>
{
    private readonly IPokemonSetRepository       _setRepo;
    private readonly IPokemonCardRepository      _cardRepo;
    private readonly ICardTranslationRepository  _translationRepo;
    private readonly ITcgDexClient               _tcgDexClient;
    private readonly IUnitOfWork                 _uow;
    private readonly ILogger<SyncCardTranslationsCommandHandler> _logger;

    public SyncCardTranslationsCommandHandler(
        IPokemonSetRepository      setRepo,
        IPokemonCardRepository     cardRepo,
        ICardTranslationRepository translationRepo,
        ITcgDexClient              tcgDexClient,
        IUnitOfWork                uow,
        ILogger<SyncCardTranslationsCommandHandler> logger)
    {
        _setRepo         = setRepo;
        _cardRepo        = cardRepo;
        _translationRepo = translationRepo;
        _tcgDexClient    = tcgDexClient;
        _uow             = uow;
        _logger          = logger;
    }

    public async Task<SyncCardTranslationsResultDto> Handle(
        SyncCardTranslationsCommand request, CancellationToken cancellationToken)
    {
        var language = request.Language.Trim().ToLowerInvariant();
        var errors   = new List<string>();
        int setsProcessed = 0, translationsUpserted = 0;

        IReadOnlyList<PokemonSet> sets;
        if (request.SetTcgId is not null)
        {
            var single = await _setRepo.GetByTcgIdAsync(request.SetTcgId, cancellationToken);
            if (single is null)
                return new SyncCardTranslationsResultDto(0, 0, [$"Set TCG introuvable : {request.SetTcgId}"]);
            sets = [single];
        }
        else
        {
            sets = await _setRepo.ListAsync(cancellationToken);
        }

        foreach (var set in sets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cardIdsToTranslate = request.ForceRefresh
                ? null
                : await _translationRepo.GetCardIdsWithoutTranslationAsync(set.Id, language, cancellationToken);

            var cardsInSet = await _cardRepo.GetIdAndTcgIdBySetIdAsync(set.Id, cancellationToken);
            if (cardsInSet.Count == 0)
                continue;

            var candidateCards = request.ForceRefresh
                ? cardsInSet
                : cardsInSet.Where(c => cardIdsToTranslate!.Contains(c.Id)).ToList();

            if (candidateCards.Count == 0)
            {
                _logger.LogDebug(
                    "Set {SetId} : toutes les cartes ont déjà une traduction '{Language}', skip.",
                    set.TcgId, language);
                continue;
            }

            IReadOnlyList<TcgDexCardNameDto> tcgDexNames;
            try
            {
                tcgDexNames = await _tcgDexClient.GetCardNamesBySetAsync(set.TcgId, language, cancellationToken);
            }
            catch (Exception ex)
            {
                var msg = $"TCGdex a échoué sur le set '{set.TcgId}' ({language}) : {ex.Message}";
                _logger.LogWarning(ex, "{Message}", msg);
                errors.Add(msg);
                continue;
            }

            if (tcgDexNames.Count == 0)
            {
                _logger.LogDebug(
                    "Set {SetId} : aucun nom '{Language}' retourné par TCGdex, skip.",
                    set.TcgId, language);
                continue;
            }

            var nameByTcgId = tcgDexNames
                .GroupBy(n => n.TcgId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Name, StringComparer.OrdinalIgnoreCase);

            var toUpsert = new List<CardTranslation>();
            foreach (var (cardId, tcgId) in candidateCards)
            {
                if (nameByTcgId.TryGetValue(tcgId, out var translatedName) &&
                    !string.IsNullOrWhiteSpace(translatedName))
                {
                    toUpsert.Add(CardTranslation.Create(cardId, language, translatedName));
                }
            }

            if (toUpsert.Count == 0)
                continue;

            await _translationRepo.UpsertRangeAsync(toUpsert, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            translationsUpserted += toUpsert.Count;
            setsProcessed++;

            _logger.LogInformation(
                "Traductions '{Language}' sync pour le set {SetId} : {Count} cartes.",
                language, set.TcgId, toUpsert.Count);
        }

        _logger.LogInformation(
            "SyncCardTranslations terminé ({Language}). Sets={Sets} Traductions={Count} Erreurs={Errors}",
            language, setsProcessed, translationsUpserted, errors.Count);

        return new SyncCardTranslationsResultDto(setsProcessed, translationsUpserted, errors);
    }
}
