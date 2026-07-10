using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Common;
using Pokekool.Domain.Repositories;

namespace Pokekool.Application.Shop.Commands.CancelMyAppointment;

/// <summary>
/// Annulation d'un RDV par le client. Vérifie que le RDV appartient bien au compte
/// (email JWT), qu'il est encore Pending et à plus de 24h dans le futur.
/// </summary>
public sealed record CancelMyAppointmentCommand(Guid AppointmentId) : IRequest;

public sealed class CancelMyAppointmentCommandHandler : IRequestHandler<CancelMyAppointmentCommand>
{
    private readonly IShopClientRepository _repo;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public CancelMyAppointmentCommandHandler(IShopClientRepository repo, ICurrentUserService currentUser, IUnitOfWork uow)
    {
        _repo = repo;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task Handle(CancelMyAppointmentCommand request, CancellationToken cancellationToken)
    {
        var email = _currentUser.Email
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var appt = await _repo.GetAppointmentOwnershipAsync(request.AppointmentId, cancellationToken)
            ?? throw new DomainException("shop.appointment_not_found", "Appointment not found.");

        if (!string.Equals(appt.ClientEmail, email, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("shop.appointment_forbidden", "This appointment does not belong to you.");

        if (!string.Equals(appt.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            throw new DomainException("shop.appointment_not_cancellable", "Only pending appointments can be cancelled.");

        if (appt.ScheduledAt <= DateTimeOffset.UtcNow.AddHours(24))
            throw new DomainException("shop.appointment_too_late", "Appointments can only be cancelled more than 24h in advance.");

        await _repo.CancelAppointmentAsync(request.AppointmentId, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
