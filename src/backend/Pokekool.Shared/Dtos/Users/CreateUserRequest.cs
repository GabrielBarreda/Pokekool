namespace Pokekool.Shared.Dtos.Users;

public sealed record CreateUserRequest(
    string Email,
    string Password,
    string? FirstName,
    string? LastName,
    string? Phone,
    IReadOnlyList<Guid> RoleIds);
