using Pokekool.Domain.Entities;

namespace Pokekool.Application.Abstractions;

public interface IJwtTokenGenerator
{
    (string AccessToken, DateTimeOffset ExpiresAt) GenerateAccessToken(User user, IEnumerable<string> roles);
    string GenerateRefreshToken();
}
