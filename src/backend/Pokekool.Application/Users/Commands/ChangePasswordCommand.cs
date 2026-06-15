using FluentValidation;
using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Repositories;
using Pokekool.Shared.Dtos.Users;

namespace Pokekool.Application.Users.Commands;

public sealed record ChangePasswordCommand(ChangePasswordRequest Request) : IRequest;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(c => c.Request.CurrentPassword).NotEmpty();
        RuleFor(c => c.Request.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(200);
    }
}

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IUserRepository _users;
    private readonly ICurrentUserService _currentUser;
    private readonly IPasswordHasher _hasher;
    private readonly IUnitOfWork _uow;

    public ChangePasswordCommandHandler(
        IUserRepository users,
        ICurrentUserService currentUser,
        IPasswordHasher hasher,
        IUnitOfWork uow)
    {
        _users = users;
        _currentUser = currentUser;
        _hasher = hasher;
        _uow = uow;
    }

    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null) throw UserErrors.UserNotFound(Guid.Empty);

        var user = await _users.GetByIdAsync(_currentUser.UserId.Value, cancellationToken)
            ?? throw UserErrors.UserNotFound(_currentUser.UserId.Value);

        if (!_hasher.Verify(request.Request.CurrentPassword, user.PasswordHash))
            throw UserErrors.InvalidCurrentPassword();

        user.ChangePassword(_hasher.Hash(request.Request.NewPassword));
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
