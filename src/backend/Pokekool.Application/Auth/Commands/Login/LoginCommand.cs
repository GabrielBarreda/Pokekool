using MediatR;
using Pokekool.Shared.Dtos.Auth;

namespace Pokekool.Application.Auth.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<LoginResponse>;
