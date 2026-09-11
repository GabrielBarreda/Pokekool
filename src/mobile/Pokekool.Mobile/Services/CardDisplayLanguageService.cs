namespace Pokekool.Mobile.Services;

/// <summary>
/// Préférence de langue d'affichage des noms de cartes — persistée localement (Preferences)
/// uniquement. Ne résout AUCUN nom lui-même : la résolution (DisplayName) se fait côté backend
/// à partir de card_translations, via le header envoyé par DisplayLanguageHttpMessageHandler.
/// Voir ADR-004 (mémoire Obsidian du projet).
/// </summary>
public sealed class CardDisplayLanguageService
{
    private const string PreferenceKey = "card_display_language";
    private const string DefaultLanguage = "en";

    public string Language
    {
        get => Preferences.Default.Get(PreferenceKey, DefaultLanguage);
        set
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? DefaultLanguage : value.Trim().ToLowerInvariant();
            Preferences.Default.Set(PreferenceKey, normalized);
        }
    }
}
