using MediatR;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Commands.CreateShopProduct;

public sealed record CreateShopProductCommand(
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
