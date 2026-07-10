using Pokekool.Domain.Entities.Shop;

namespace Pokekool.Application.Shop;

/// <summary>Conversions string &lt;-&gt; enums du module Boutique (tolérantes à la casse).</summary>
internal static class ShopEnumMap
{
    public static ShopProductType ParseProductType(string value)
        => Enum.TryParse<ShopProductType>(value, ignoreCase: true, out var v)
            ? v : throw ShopErrors.InvalidEnum("type", value);

    public static ShopProductType? ParseProductTypeOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : ParseProductType(value);

    public static ShopServiceType ParseServiceType(string value)
        => Enum.TryParse<ShopServiceType>(value, ignoreCase: true, out var v)
            ? v : throw ShopErrors.InvalidEnum("serviceType", value);

    public static ShopServiceType? ParseServiceTypeOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : ParseServiceType(value);

    public static AppointmentStatus ParseStatus(string value)
        => Enum.TryParse<AppointmentStatus>(value, ignoreCase: true, out var v)
            ? v : throw ShopErrors.InvalidEnum("status", value);

    public static AppointmentStatus? ParseStatusOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : ParseStatus(value);
}
