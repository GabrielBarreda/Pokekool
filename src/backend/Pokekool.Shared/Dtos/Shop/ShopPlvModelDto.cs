namespace Pokekool.Shared.Dtos.Shop;

/// <summary>
/// Payload léger exposant uniquement l'URL du modèle 3D de la PLV (présentoir), accessible
/// en lecture (shop:read) par le viewer de la page Présentoir — sans nécessiter shop:manage.
/// </summary>
public sealed record ShopPlvModelDto(string? PlvModel3dUrl);
