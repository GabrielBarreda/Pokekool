using MediatR;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Commands.CreateShopStock;

public sealed record CreateShopStockCommand(
    Guid? CardId,
    string? ProductName,
    string Type,
    string? Condition,
    string? GradingCompany,
    decimal? GradeValue,
    int QuantityTotal,
    int QuantityAvailable,
    int QuantityReserved,
    decimal? PurchasePrice,
    string? PurchaseCurrency,
    string? Notes) : IRequest<ShopStockDto>;
