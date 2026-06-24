using MediatR;
using Pokekool.Application.Abstractions;

namespace Pokekool.Application.Settings.Queries.GetProviderSettings;

internal sealed class GetProviderSettingsQueryHandler
    : IRequestHandler<GetProviderSettingsQuery, ProviderSettingsDto>
{
    private readonly IProviderSettingsService _settings;

    // Clés attendues par provider — sert à afficher "non configuré" même si absent en base
    private static readonly IReadOnlyDictionary<string, string[]> ExpectedKeys =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["cardmarket"]             = ["app_token", "app_secret", "access_token", "access_secret"],
            ["cardmarket-scraping"]    = [],   // scraping public — aucune clé
            ["pricecharting"]          = ["api_key", "extraction_mode"],
            ["ebay"]                   = ["app_id"],
            ["tcgplayer"]              = ["public_key", "private_key"],
            ["pokemon-tcg"]            = [],   // API publique — aucune clé
            ["cardtrader"]             = [],   // scraping public — aucune clé
            ["mavin"]                  = [],   // scraping public — aucune clé
            ["130point"]               = [],   // scraping public — aucune clé
            ["psa"]                    = [],   // scraping public — aucune clé
        };

    // Clés dont la valeur peut être renvoyée en clair (non-sensibles)
    private static readonly HashSet<string> NonSecretKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "extraction_mode",
    };

    public GetProviderSettingsQueryHandler(IProviderSettingsService settings)
        => _settings = settings;

    public async Task<ProviderSettingsDto> Handle(GetProviderSettingsQuery request, CancellationToken ct)
    {
        var provider = request.Provider.ToLowerInvariant();
        var configured = await _settings.GetConfiguredKeysAsync(provider, ct);
        var configuredSet = configured.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var expectedKeys = ExpectedKeys.GetValueOrDefault(provider, []);

        // Union des clés attendues + clés déjà en base
        var keyNames = expectedKeys
            .Union(configured, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Résoudre les valeurs non-sensibles en parallèle
        var allKeys = new List<ProviderSettingKeyDto>(keyNames.Count);
        foreach (var k in keyNames)
        {
            string? value = null;
            if (NonSecretKeys.Contains(k))
                value = await _settings.GetAsync(provider, k, ct);

            allKeys.Add(new ProviderSettingKeyDto(k, configuredSet.Contains(k), value));
        }

        return new ProviderSettingsDto(provider, allKeys);
    }
}
