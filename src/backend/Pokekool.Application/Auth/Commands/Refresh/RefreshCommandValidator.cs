using FluentValidation;

namespace Pokekool.Application.Auth.Commands.Refresh;

public sealed class RefreshCommandValidator : AbstractValidator<RefreshCommand>
{
    public RefreshCommandValidator()
    {
        RuleFor(c => c.RefreshToken)
            .NotEmpty().WithMessage("RefreshToken is required.")
            .MaximumLength(500);
    }
}
