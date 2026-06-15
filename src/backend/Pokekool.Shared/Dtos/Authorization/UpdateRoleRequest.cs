namespace Pokekool.Shared.Dtos.Authorization;

public sealed record UpdateRoleRequest(
    string Name,
    string? Description,
    IReadOnlyList<string> Permissions);
