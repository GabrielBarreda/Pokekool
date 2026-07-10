namespace Pokekool.Shared.Dtos.Shop;

public sealed record ShopAppointmentDto(
    Guid Id,
    Guid ShopId,
    string ClientName,
    string ClientEmail,
    string? ClientPhone,
    string ServiceType,
    DateTimeOffset ScheduledAt,
    int DurationMinutes,
    string Status,
    string? NotesClient,
    string? NotesInternal,
    Guid? AssignedToUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
