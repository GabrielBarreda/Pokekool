using MediatR;
using Pokekool.Domain.Repositories;

namespace Pokekool.Application.Shop.Commands.DeleteShopStock;

internal sealed class DeleteShopStockCommandHandler : IRequestHandler<DeleteShopStockCommand, Unit>
{
    private readonly IShopStockRepository _repo;
    private readonly IUnitOfWork _uow;

    public DeleteShopStockCommandHandler(IShopStockRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<Unit> Handle(DeleteShopStockCommand request, CancellationToken ct)
    {
        var stock = await _repo.GetByIdAsync(request.Id, ct)
            ?? throw ShopErrors.StockNotFound(request.Id);

        _repo.Remove(stock);
        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
