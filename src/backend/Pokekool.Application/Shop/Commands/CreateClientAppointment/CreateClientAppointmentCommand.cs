using FluentValidation;
using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Common;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Shop;

namespace Pokekool.Application.Shop.Commands.CreateClientAppointment;

/// <summary>
/// Prise de RDV par un client authentifié. UserId issu du JWT ; nom/email copiés du compte.
/// ServiceType attendu : "pre_grading" | "restoration" (config statique V1).
/// </summary>
public sealed record CreateClientAppointmentCommand(
    string ServiceType,
    DateTimeOffset ScheduledAt,
    string? NotesClient) : IRequest<ShopAppointmentDto>;

public sealed class CreateClientAppointmentCommandValidator : AbstractValidator<CreateClientAppointmentCommand>
{
    private static readonly string[] Allowed = { "pre_grading", "restoration" };

    public CreateClientAppointmentCommandValidator()
    {
        RuleFor(c => c.ServiceType)
            .NotEmpty()
            .Must(s => Allowed.Contains(s.ToLowerInvariant()))
            .WithMessage("ServiceType must be one of: pre_grading, restoration.");
        RuleFor(c => c.ScheduledAt)
            .Must(d => d > DateTimeOffset.UtcNow)
            .WithMessage("ScheduledAt must be in the future.");
        RuleFor(c => c.NotesClient).MaximumLength(2000);
    }
}

public sealed class CreateClientAppointmentCommandHandler : IRequestHandler<CreateClientAppointmentCommand, ShopAppointmentDto>
{
    private const int DefaultDurationMinutes = 60;

    private readonly IShopClientRepository _shop;
    private readonly IUserRepository _users;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public CreateClientAppointmentCommandHandler(
        IShopClientRepository shop,
        IUserRepository users,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _shop = shop;
        _users = users;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<ShopAppointmentDto> Handle(CreateClientAppointmentCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var shop = await _shop.GetShopHoursAsync(cancellationToken)
            ?? throw new DomainException("shop.not_configured", "Shop is not configured yet.");

        var user = await _users.GetByIdAsync(userId, cancellationToken)
            ?? throw new DomainException("account.not_found", "User not found.");

        var clientName = $"{user.FirstName} {user.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(clientName)) clientName = user.Email.Value;

        var appointment = new NewClientAppointment(
            shop.ShopId,
            userId,
            clientName,
            user.Email.Value,
            user.Phone,
            MapServiceType(request.ServiceType),
            request.ScheduledAt,
            DefaultDurationMinutes,
            request.NotesClient);

        var dto = await _shop.AddAppointmentAsync(appointment, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return dto;
    }

    // Mappe le code client vers le nom d'enum ShopServiceType (module Boutique).
    private static string MapServiceType(string serviceType) => serviceType.ToLowerInvariant() switch
    {
        "pre_grading" => "PreGrading",
        "restoration" => "Restoration",
        _ => "Other",
    };
}
