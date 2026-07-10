using Pokekool.Domain.Common;

namespace Pokekool.Application.Shop;

public static class ShopErrors
{
    public static DomainException ProductNotFound(Guid id)
        => new("shop_product.not_found", $"Shop product '{id}' not found.");

    public static DomainException StockNotFound(Guid id)
        => new("shop_stock.not_found", $"Shop stock entry '{id}' not found.");

    public static DomainException AppointmentNotFound(Guid id)
        => new("shop_appointment.not_found", $"Shop appointment '{id}' not found.");

    public static DomainException InvalidEnum(string field, string value)
        => new("shop.invalid_enum", $"Invalid value '{value}' for '{field}'.");
}
