namespace Pokekool.Shared.Dtos.Authorization;

public sealed record PermissionDto(
    string Code,
    string Category,
    string Description);
