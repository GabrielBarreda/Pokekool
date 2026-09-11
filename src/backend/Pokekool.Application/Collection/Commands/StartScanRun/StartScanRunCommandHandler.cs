using MediatR;
using Pokekool.Domain.Entities.Collection;
using Pokekool.Domain.Repositories;

namespace Pokekool.Application.Collection.Commands.StartScanRun;

public sealed class StartScanRunCommandHandler : IRequestHandler<StartScanRunCommand, Guid>
{
    private readonly IScanRunRepository _scanRunRepo;
    private readonly IUnitOfWork _uow;

    public StartScanRunCommandHandler(IScanRunRepository scanRunRepo, IUnitOfWork uow)
    {
        _scanRunRepo = scanRunRepo;
        _uow = uow;
    }

    public async Task<Guid> Handle(StartScanRunCommand request, CancellationToken cancellationToken)
    {
        var scanRun = ScanRun.Create(
            request.DestinationType,
            request.AddedToStockDirectly,
            request.ThirdPartyName,
            request.Reason,
            request.ReasonOtherText);

        await _scanRunRepo.AddAsync(scanRun, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return scanRun.Id;
    }
}
