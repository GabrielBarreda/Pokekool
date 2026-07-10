using MediatR;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Commands.UpdateShopProduct;

public sealed record UpdateShopProductCommand(
    Guid Id,
    string Type,
    Guid? CardId,
    string? ProductName,
    string? Condition,
    string? GradingCompany,
    decimal? GradeValue,
    decimal Price,
    string? Currency,
    int Quantity,
    bool IsAvailable,
    string? Description,
    string? PhotoUrl) : IRequest<ShopProductDto>;
