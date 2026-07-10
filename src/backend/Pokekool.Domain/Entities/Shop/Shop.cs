using Pokekool.Domain.Common;

namespace Pokekool.Domain.Entities.Shop;

/// <summary>
/// Boutique physique — entité singleton (une seule par déploiement).
/// Créée automatiquement (vide) au premier accès via GetOrCreateAsync.
/// </summary>
public sealed class Shop : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Address { get; private set; }
    public string? City { get; private set; }
    public string? ZipCode { get; private set; }
    public string? GoogleMapsUrl { get; private set; }

    /// <summary>Horaires hebdomadaires sérialisés en JSON (jsonb).</summary>
    public string? HoursJson { get; private set; }

    /// <summary>Prestations proposées : ["vente", "pre_grading", "restoration"].</summary>
    public string[] Services { get; private set; } = Array.Empty<string>();

    public string? InstagramUrl { get; private set; }
    public string? TiktokUrl { get; private set; }
    public string[] PhotoUrls { get; private set; } = Array.Empty<string>();

    private Shop() { }

    /// <summary>Crée une boutique vide (singleton initial).</summary>
    public static Shop CreateEmpty() => new()
    {
        Id = Guid.NewGuid(),
        Name = string.Empty,
        Services = Array.Empty<string>(),
        PhotoUrls = Array.Empty<string>(),
        CreatedAt = DateTimeOffset.UtcNow,
    };

    public void Update(
        string name,
        string? description,
        string? address,
        string? city,
        string? zipCode,
        string? googleMapsUrl,
        string? hoursJson,
        string[]? services,
        string? instagramUrl,
        string? tiktokUrl,
        string[]? photoUrls)
    {
        Name = (name ?? string.Empty).Trim();
        Description = description;
        Address = address;
        City = city;
        ZipCode = zipCode;
        GoogleMapsUrl = googleMapsUrl;
        HoursJson = hoursJson;
        Services = services ?? Array.Empty<string>();
        InstagramUrl = instagramUrl;
        TiktokUrl = tiktokUrl;
        PhotoUrls = photoUrls ?? Array.Empty<string>();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
