namespace Pokekool.Shared.Dtos.Authorization;

public sealed record RoleDto(
    Guid Id,
    Guid? TenantId,
    string Code,
    string Name,
    string? Description,
    bool IsSystem,
    IReadOnlyList<string> Permissions,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
