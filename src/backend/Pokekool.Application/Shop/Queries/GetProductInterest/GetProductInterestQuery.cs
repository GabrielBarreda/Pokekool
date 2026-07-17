using MediatR;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Queries.GetProductInterest;

/// <summary>Nombre de clients intéressés (demandes Pending/Contacted) par un article.</summary>
public sealed record GetProductInterestQuery(Guid ProductId) : IRequest<ProductInterestDto>;

internal sealed class GetProductInterestQueryHandler
    : IRequestHandler<GetProductInterestQuery, ProductInterestDto>
{
    private readonly IPurchaseRequestRepository _repo;

    public GetProductInterestQueryHandler(IPurchaseRequestRepository repo) => _repo = repo;

    public async Task<ProductInterestDto> Handle(GetProductInterestQuery request, CancellationToken ct)
    {
        var count = await _repo.CountActiveByProductAsync(request.ProductId, ct);
        return new ProductInterestDto(request.ProductId, count);
    }
}
