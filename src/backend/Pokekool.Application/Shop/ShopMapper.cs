using Pokekool.Domain.Entities.Pokemon;
using Pokekool.Domain.Entities.Shop;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop;

/// <summary>Mappings entités -> DTOs du module Boutique.</summary>
internal static class ShopMapper
{
    public static ShopDto ToDto(Domain.Entities.Shop.Shop s) => new(
        s.Id, s.Name, s.Description, s.Address, s.City, s.ZipCode, s.GoogleMapsUrl,
        s.HoursJson, s.Services, s.InstagramUrl, s.TiktokUrl, s.PhotoUrls,
        s.PlvModel3dUrl, s.CreatedAt, s.UpdatedAt);

    // displayName est résolu en amont par l'appelant (ICardDisplayNameResolver a besoin d'un
    // appel async / de la langue courante, indisponibles ici puisque ShopMapper reste statique
    // et synchrone) — repli sur card?.Name quand aucune traduction n'a été résolue (card null,
    // ou pas de traduction connue pour la langue courante).
    public static ShopProductDto ToDto(ShopProduct p, PokemonCard? card, string? displayName) => new(
        p.Id, p.ShopId, p.Type.ToString(), p.CardId, card?.TcgId,
        card?.Name, displayName ?? card?.Name, card?.ImageSmall,
        p.ProductName, p.Language, p.Condition, p.GradingCompany, p.GradeValue,
        p.Price, p.Currency, p.Quantity, p.IsAvailable, p.Description, p.PhotoUrl,
        p.CreatedAt, p.UpdatedAt);

    public static ShopStockDto ToDto(ShopStock s, PokemonCard? card, string? displayName) => new(
        s.Id, s.ShopId, s.CardId, card?.TcgId, card?.Name, displayName ?? card?.Name, card?.ImageSmall,
        s.ProductName,
        s.Type.ToString(), s.Language, s.Condition, s.GradingCompany, s.GradeValue,
        s.QuantityTotal, s.QuantityAvailable, s.QuantityReserved,
        s.PurchasePrice, s.PurchaseCurrency, s.Notes, s.CreatedAt, s.UpdatedAt);

    public static ShopAppointmentDto ToDto(ShopAppointment a) => new(
        a.Id, a.ShopId, a.ClientName, a.ClientEmail, a.ClientPhone,
        a.ServiceType.ToString(), a.ScheduledAt, a.DurationMinutes, a.Status.ToString(),
        a.NotesClient, a.NotesInternal, a.AssignedToUserId, a.CreatedAt, a.UpdatedAt);
}
