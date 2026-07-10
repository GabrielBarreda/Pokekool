using MediatR;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Queries.GetShopInfo;

/// <summary>Retourne la boutique singleton (créée vide si absente).</summary>
public sealed record GetShopInfoQuery : IRequest<ShopDto>;
