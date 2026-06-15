namespace Pokekool.Shared.Dtos.Users;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
