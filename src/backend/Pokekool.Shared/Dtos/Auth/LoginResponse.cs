namespace Pokekool.Shared.Dtos.Auth;

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    UserSummaryDto User);
