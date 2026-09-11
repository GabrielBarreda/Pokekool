using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Application.Pokemon.Services;
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
    private readonly ICardDisplayNameResolver _displayNameResolver;
    private readonly ICurrentDisplayLanguageService _displayLanguage;

    public GetBinderCardsQueryHandler(
        IStorageLocationRepository locationRepo,
        IShopStockRepository stockRepo,
        IPokemonCardRepository cardRepo,
        ICardDisplayNameResolver displayNameResolver,
        ICurrentDisplayLanguageService displayLanguage)
    {
        _locationRepo = locationRepo;
        _stockRepo = stockRepo;
        _cardRepo = cardRepo;
        _displayNameResolver = displayNameResolver;
        _displayLanguage = displayLanguage;
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
                // DisplayName n'a de sens que pour une carte du référentiel (card non null) :
                // sinon (article scellé/sans carte liée), pas de traduction possible.
                var displayName = card is not null
                    ? await _displayNameResolver.ResolveAsync(card.Id, card.Name, _displayLanguage.Language, ct)
                    : name;
                result.Add(new StorageBinderCardDto(
                    loc.Id, loc.OwnerType, loc.OwnerId, stock?.CardId, card?.TcgId, name, displayName,
                    card?.ImageSmall, card?.ImageLarge, card?.Set?.Name,
                    stock?.Condition, stock?.GradingCompany, stock?.GradeValue,
                    loc.SlotPosition ?? 0));
            }
            else
            {
                // user_collection (et autres) : mapping minimal pour l'instant. Pas de carte
                // du référentiel identifiée ici, donc pas de traduction possible.
                result.Add(new StorageBinderCardDto(
                    loc.Id, loc.OwnerType, loc.OwnerId, null, null, "Carte (collection)", "Carte (collection)",
                    null, null, null, null, null, null, loc.SlotPosition ?? 0));
            }
        }

        return result;
    }
}
