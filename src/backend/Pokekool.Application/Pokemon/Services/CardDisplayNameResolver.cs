using Pokekool.Domain.Repositories;

namespace Pokekool.Application.Pokemon.Services;

public sealed class CardDisplayNameResolver : ICardDisplayNameResolver
{
    private const string EnglishLanguage = "en";

    private readonly ICardTranslationRepository _translationRepo;

    public CardDisplayNameResolver(ICardTranslationRepository translationRepo)
        => _translationRepo = translationRepo;

    public async Task<string> ResolveAsync(
        Guid cardId, string englishName, string language, CancellationToken ct = default)
    {
        if (string.Equals(language, EnglishLanguage, StringComparison.OrdinalIgnoreCase))
            return englishName;

        var names = await _translationRepo.GetNamesByCardIdsAsync([cardId], language, ct);
        return names.TryGetValue(cardId, out var translated) ? translated : englishName;
    }

    public async Task<IReadOnlyDictionary<Guid, string>> ResolveManyAsync(
        IReadOnlyDictionary<Guid, string> englishNamesByCardId, string language, CancellationToken ct = default)
    {
        if (string.Equals(language, EnglishLanguage, StringComparison.OrdinalIgnoreCase) ||
            englishNamesByCardId.Count == 0)
        {
            return englishNamesByCardId;
        }

        var translated = await _translationRepo.GetNamesByCardIdsAsync(
            englishNamesByCardId.Keys.ToList(), language, ct);

        return englishNamesByCardId.ToDictionary(
            kvp => kvp.Key,
            kvp => translated.TryGetValue(kvp.Key, out var name) ? name : kvp.Value);
    }
}
