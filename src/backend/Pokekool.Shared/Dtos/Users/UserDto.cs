namespace Pokekool.Shared.Dtos.Users;

public sealed record UserDto(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    string? Phone,
    bool IsActive,
    bool EmailConfirmed,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<Guid> RoleIds,
    IReadOnlyList<string> RoleCodes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
