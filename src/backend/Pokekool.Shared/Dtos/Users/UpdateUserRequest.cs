namespace Pokekool.Shared.Dtos.Users;

public sealed record UpdateUserRequest(
    string? FirstName,
    string? LastName,
    string? Phone);
