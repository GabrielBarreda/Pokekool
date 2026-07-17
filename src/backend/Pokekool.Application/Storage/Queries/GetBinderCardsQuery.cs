using MediatR;
using Pokekool.Domain.Entities.Storage;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.Repositories.Storage;
using Pokekool.Shared.Dtos.Storage;

namespace Pokekool.Application.Storage.Queries;

/// <summary>Cartes rangées dans un classeur : StorageLocation → ShopStock → PokemonCard.</summary>
public sealed record GetBinderCardsQuery(Guid BinderId) : IRequest<IReadOnlyList<StorageBinderCardDto>>;

internal sealed class GetBinderCardsQueryHandler
    : IRequestHandler<GetBinderCardsQuery, IReadOnlyList<StorageBinderCardDto>>
{
    private readonly IStorageLocationRepository _locationRepo;
    private readonly IShopStockRepository _stockRepo;
    private readonly IPokemonCardRepository _cardRepo;

    public GetBinderCardsQueryHandler(
        IStorageLocationRepository locationRepo,
        IShopStockRepository stockRepo,
        IPokemonCardRepository cardRepo)
    {
        _locationRepo = locationRepo;
        _stockRepo = stockRepo;
        _cardRepo = cardRepo;
    }

    public async Task<IReadOnlyList<StorageBinderCardDto>> Handle(GetBinderCardsQuery request, CancellationToken ct)
    {
        var locations = await _locationRepo.ListByBinderAsync(request.BinderId, ct);
        var result = new List<StorageBinderCardDto>(locations.Count);

        foreach (var loc in locations)
        {
            if (loc.OwnerType == StorageLocation.OwnerShopStock)
            {
                var stock = await _stockRepo.GetByIdAsync(loc.OwnerId, ct);
                var card = stock?.CardId is { } cid ? await _cardRepo.GetByIdAsync(cid, ct) : null;
                var name = card?.Name ?? stock?.ProductName ?? "Article";
                result.Add(new StorageBinderCardDto(
                    loc.Id, loc.OwnerType, loc.OwnerId, stock?.CardId, name,
                    card?.ImageSmall, card?.ImageLarge, card?.Set?.Name,
                    stock?.Condition, stock?.GradingCompany, stock?.GradeValue,
                    loc.SlotPosition ?? 0));
            }
            else
            {
                // user_collection (et autres) : mapping minimal pour l'instant.
                result.Add(new StorageBinderCardDto(
                    loc.Id, loc.OwnerType, loc.OwnerId, null, "Carte (collection)",
                    null, null, null, null, null, null, loc.SlotPosition ?? 0));
            }
        }

        return result;
    }
}
