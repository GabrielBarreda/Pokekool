using System.Security.Cryptography;
using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Users;

namespace Pokekool.Application.Users.Commands;

public sealed record ResetPasswordCommand(Guid UserId) : IRequest<ResetPasswordResponse>;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResponse>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IUnitOfWork _uow;

    public ResetPasswordCommandHandler(IUserRepository users, IPasswordHasher hasher, IUnitOfWork uow)
    {
        _users = users;
        _hasher = hasher;
        _uow = uow;
    }

    public async Task<ResetPasswordResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw UserErrors.UserNotFound(request.UserId);

        var tempPassword = GenerateTempPassword();
        user.ChangePassword(_hasher.Hash(tempPassword));
        await _uow.SaveChangesAsync(cancellationToken);

        return new ResetPasswordResponse(tempPassword);
    }

    /// <summary>Génère un mot de passe temporaire 12 caractères : 3 segments alphanumériques séparés.</summary>
    private static string GenerateTempPassword()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // sans O/0/I/1 pour lisibilité
        var bytes = RandomNumberGenerator.GetBytes(12);
        var chars = new char[12];
        for (var i = 0; i < 12; i++) chars[i] = alphabet[bytes[i] % alphabet.Length];
        return $"{new string(chars, 0, 4)}-{new string(chars, 4, 4)}-{new string(chars, 8, 4)}";
    }
}
