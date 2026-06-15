namespace Pokekool.Shared.Dtos.Auth;

public sealed record UserSummaryDto(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    Guid? TenantId,
    IReadOnlyList<string> Roles);
