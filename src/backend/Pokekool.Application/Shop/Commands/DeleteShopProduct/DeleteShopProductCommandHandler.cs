using MediatR;
using Pokekool.Domain.Repositories;

namespace Pokekool.Application.Shop.Commands.DeleteShopProduct;

internal sealed class DeleteShopProductCommandHandler : IRequestHandler<DeleteShopProductCommand, Unit>
{
    private readonly IShopProductRepository _repo;
    private readonly IUnitOfWork _uow;

    public DeleteShopProductCommandHandler(IShopProductRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<Unit> Handle(DeleteShopProductCommand request, CancellationToken ct)
    {
        var product = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw ShopErrors.ProductNotFound(request.Id);

        _repo.Remove(product);
        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
