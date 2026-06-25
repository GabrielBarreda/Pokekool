namespace Pokekool.Shared.Dtos.Users;

public sealed record UserSummaryDto(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    bool IsActive,
    IReadOnlyList<string> Roles,
    bool HasPendingMessages,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt
);
