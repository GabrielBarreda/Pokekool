using MediatR;

namespace Pokekool.Application.Pokemon.Commands.SyncCardTranslations;

/// <param name="Language">Langue cible (ex. "fr"). Toujours passée en paramètre — jamais en
/// littéral codé en dur dans une requête SQL (voir ADR-004, mémoire Obsidian du projet).</param>
/// <param name="SetTcgId">Si fourni, ne sync que ce set. Sinon, tous les sets existants.</param>
/// <param name="ForceRefresh">Re-traduit même les cartes ayant déjà une traduction pour cette langue.</param>
public sealed record SyncCardTranslationsCommand(
    string Language,
    string? SetTcgId = null,
    bool ForceRefresh = false) : IRequest<SyncCardTranslationsResultDto>;

public sealed record SyncCardTranslationsResultDto(
    int SetsProcessed,
    int TranslationsUpserted,
    IReadOnlyList<string> Errors);
