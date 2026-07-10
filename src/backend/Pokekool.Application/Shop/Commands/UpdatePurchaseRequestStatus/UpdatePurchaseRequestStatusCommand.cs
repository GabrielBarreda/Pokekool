using FluentValidation;
using MediatR;
using Pokekool.Domain.Common;
using Pokekool.Domain.Entities.Shop;
using Pokekool.Domain.Repositories;

namespace Pokekool.Application.Shop.Commands.UpdatePurchaseRequestStatus;

/// <summary>Admin : change le statut d'une demande d'achat.</summary>
public sealed record UpdatePurchaseRequestStatusCommand(Guid Id, string Status) : IRequest;

public sealed class UpdatePurchaseRequestStatusCommandValidator : AbstractValidator<UpdatePurchaseRequestStatusCommand>
{
    public UpdatePurchaseRequestStatusCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Status)
            .NotEmpty()
            .Must(s => Enum.TryParse<PurchaseRequestStatus>(s, ignoreCase: true, out _))
            .WithMessage("Status must be one of: Pending, Contacted, Completed, Cancelled.");
    }
}

public sealed class UpdatePurchaseRequestStatusCommandHandler : IRequestHandler<UpdatePurchaseRequestStatusCommand>
{
    private readonly IPurchaseRequestRepository _repo;
    private readonly IUnitOfWork _uow;

    public UpdatePurchaseRequestStatusCommandHandler(IPurchaseRequestRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task Handle(UpdatePurchaseRequestStatusCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new DomainException("shop.purchase_request_not_found", "Purchase request not found.");

        var status = Enum.Parse<PurchaseRequestStatus>(request.Status, ignoreCase: true);
        entity.ChangeStatus(status);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
