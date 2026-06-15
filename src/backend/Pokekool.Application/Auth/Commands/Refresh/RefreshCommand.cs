using MediatR;
using Pokekool.Shared.Dtos.Auth;

namespace Pokekool.Application.Auth.Commands.Refresh;

public sealed record RefreshCommand(string RefreshToken) : IRequest<LoginResponse>;
