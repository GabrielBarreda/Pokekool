namespace Pokekool.Shared.Dtos.Authorization;

public sealed record CreateRoleRequest(
    string Code,
    string Name,
    string? Description,
    IReadOnlyList<string>? Permissions);
