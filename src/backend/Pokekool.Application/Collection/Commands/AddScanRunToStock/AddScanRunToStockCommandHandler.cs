using MediatR;
using Pokekool.Application.Collection.Scanning;
using Pokekool.Domain.Entities.Collection;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Collection;
using ScanErrors = Pokekool.Domain.Entities.Collection.ScanErrors;

namespace Pokekool.Application.Collection.Commands.AddScanRunToStock;

public sealed class AddScanRunToStockCommandHandler
    : IRequestHandler<AddScanRunToStockCommand, AddScanRunToStockResult>
{
    private readonly IScanRunRepository _scanRunRepo;
    private readonly IScanRunStockCommitter _committer;
    private readonly IUnitOfWork _uow;

    public AddScanRunToStockCommandHandler(
        IScanRunRepository scanRunRepo, IScanRunStockCommitter committer, IUnitOfWork uow)
    {
        _scanRunRepo = scanRunRepo;
        _committer = committer;
        _uow = uow;
    }

    public async Task<AddScanRunToStockResult> Handle(
        AddScanRunToStockCommand request, CancellationToken cancellationToken)
    {
        var scanRun = await _scanRunRepo.GetWithItemsAsync(request.ScanRunId, cancellationToken)
            ?? throw ScanErrors.RunNotFound(request.ScanRunId);

        if (scanRun.DestinationType != ScanDestinationType.Boutique)
            throw new Pokekool.Domain.Common.DomainException(
                "scan_run.invalid", "L'ajout au stock ne s'applique qu'à un run de destination Boutique.");

        // Idempotence gérée par le committer (no-op si CreatedBinderId déjà renseigné) — on relit
        // l'état avant/après pour distinguer "déjà commité" de "vient d'être commité" côté DTO.
        var alreadyCommitted = scanRun.CreatedBinderId is not null;

        var addedCount = await _committer.CommitAsync(scanRun, cancellationToken);

        if (addedCount > 0)
            await _uow.SaveChangesAsync(cancellationToken);

        return new AddScanRunToStockResult(
            scanRun.Id, scanRun.CreatedBinderId, addedCount, alreadyCommitted);
    }
}
