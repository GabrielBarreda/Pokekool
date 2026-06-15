using MediatR;
using Pokekool.Application.Abstractions;

namespace Pokekool.Application.Auth.Commands.Logout;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenStore _refreshStore;

    public LogoutCommandHandler(IRefreshTokenStore refreshStore) => _refreshStore = refreshStore;

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await _refreshStore.RevokeAsync(request.RefreshToken, cancellationToken);
    }
}
