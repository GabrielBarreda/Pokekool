using FluentValidation;

namespace Pokekool.Application.Collection.Commands.ToggleCardOwned;

public sealed class ToggleCardOwnedCommandValidator : AbstractValidator<ToggleCardOwnedCommand>
{
    public ToggleCardOwnedCommandValidator()
    {
        RuleFor(c => c.CardId).NotEmpty();
    }
}
