using Microsoft.EntityFrameworkCore;
using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Repositories;

namespace Pokekool.Infrastructure.Persistence.Repositories;

internal sealed class CardTranslationRepository : ICardTranslationRepository
{
    private readonly PokekoolDbContext _db;

    public CardTranslationRepository(PokekoolDbContext db) => _db = db;

    public async Task<IReadOnlyList<Guid>> GetCardIdsWithoutTranslationAsync(
        Guid setId, string language, CancellationToken ct = default)
    {
        var lang = language.Trim().ToLowerInvariant();

        var translatedCardIds = _db.CardTranslations
            .Where(t => t.Language == lang)
            .Select(t => t.CardId);

        return await _db.PokemonCards
            .Where(c => c.SetId == setId && !translatedCardIds.Contains(c.Id))
            .Select(c => c.Id)
            .ToListAsync(ct);
    }

    public async Task UpsertRangeAsync(IReadOnlyList<CardTranslation> translations, CancellationToken ct = default)
    {
        if (translations.Count == 0) return;

        var languages = translations.Select(t => t.Language).Distinct().ToList();
        var cardIds = translations.Select(t => t.CardId).Distinct().ToList();

        var existing = await _db.CardTranslations
            .Where(t => cardIds.Contains(t.CardId) && languages.Contains(t.Language))
            .ToDictionaryAsync(t => (t.CardId, t.Language), ct);

        foreach (var translation in translations)
        {
            if (existing.TryGetValue((translation.CardId, translation.Language), out var current))
                current.Update(translation.Name);
            else
                _db.CardTranslations.Add(translation);
        }
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetNamesByCardIdsAsync(
        IReadOnlyList<Guid> cardIds, string language, CancellationToken ct = default)
    {
        if (cardIds.Count == 0) return new Dictionary<Guid, string>();

        var lang = language.Trim().ToLowerInvariant();

        return await _db.CardTranslations
            .Where(t => cardIds.Contains(t.CardId) && t.Language == lang)
            .ToDictionaryAsync(t => t.CardId, t => t.Name, ct);
    }

    public async Task<IReadOnlyList<Guid>> SearchCardIdsByNameAsync(
        string? language, string nameQuery, int limit, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(nameQuery)) return Array.Empty<Guid>();

        var pattern = $"%{nameQuery.Trim()}%";
        var query = _db.CardTranslations.Where(t => EF.Functions.ILike(t.Name, pattern));

        // language null/vide = aucun filtre, toutes langues confondues (voir doc interface —
        // c'est le mode utilisé par le fallback vision+texte, qui ne sait pas à l'avance dans
        // quelle langue la carte scannée est imprimée).
        if (!string.IsNullOrWhiteSpace(language))
        {
            var lang = language.Trim().ToLowerInvariant();
            query = query.Where(t => t.Language == lang);
        }

        return await query
            // Les noms traduits les plus courts en tête = correspondances les plus précises
            // (un nom traduit qui contient la requête sans grand-chose d'autre autour).
            .OrderBy(t => t.Name.Length)
            .Take(limit)
            .Select(t => t.CardId)
            .ToListAsync(ct);
    }
}
