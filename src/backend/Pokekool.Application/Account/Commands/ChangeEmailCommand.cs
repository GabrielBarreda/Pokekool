using FluentValidation;
using MediatR;
using Pokekool.Application.Abstractions;
using Pokekool.Domain.Common;
using Pokekool.Domain.Repositories;
using Pokekool.Domain.ValueObjects;

namespace Pokekool.Application.Account.Commands;

public sealed record ChangeEmailCommand(string NewEmail) : IRequest;

public sealed class ChangeEmailCommandValidator : AbstractValidator<ChangeEmailCommand>
{
    public ChangeEmailCommandValidator()
    {
        RuleFor(c => c.NewEmail).NotEmpty().EmailAddress().MaximumLength(320);
    }
}

public sealed class ChangeEmailCommandHandler : IRequestHandler<ChangeEmailCommand>
{
    private readonly IUserRepository _users;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public ChangeEmailCommandHandler(IUserRepository users, ICurrentUserService currentUser, IUnitOfWork uow)
    {
        _users = users;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task Handle(ChangeEmailCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            throw new UnauthorizedAccessException("User not authenticated.");

        var newEmail = Email.Create(request.NewEmail);

        var taken = await _users.GetByEmailAsync(newEmail, cancellationToken);
        if (taken is not null && taken.Id != _currentUser.UserId.Value)
            throw new DomainException("account.email_taken", "An account with this email already exists.");

        var user = await _users.GetByIdAsync(_currentUser.UserId.Value, cancellationToken)
            ?? throw new DomainException("account.not_found", "User not found.");

        user.ChangeEmail(newEmail);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
