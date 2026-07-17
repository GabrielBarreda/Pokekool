namespace Pokekool.Mobile.Services;

/// <summary>
/// Configuration de l'URL de base de l'API.
/// L'API tourne dans Docker : port hôte 5080 → conteneur 8080 (cf. docker/docker-compose.dev.yml).
/// - Appareil physique : DevLanHost = IP LAN du PC qui héberge Docker (ipconfig → Adresse IPv4).
/// - Émulateur Android : utiliser "10.0.2.2" (alias du localhost du PC).
/// </summary>
public static class ApiConfig
{
    /// <summary>
    /// Mode dev via tunnel USB : lancer `adb reverse tcp:5080 tcp:5080` après chaque branchement,
    /// le téléphone voit alors l'API sur son propre localhost. Pas de Wi-Fi/pare-feu en jeu.
    /// Pour repasser en Wi-Fi : remplacer par l'IP LAN du PC (ex. 192.168.1.53).
    /// </summary>
    private const string DevLanHost = "127.0.0.1";

#if DEBUG
    public const string BaseUrl = $"http://{DevLanHost}:5080/";
#else
    public const string BaseUrl = "https://api.pokekool.com/";
#endif

    public const string V1 = "api/v1/";
}
