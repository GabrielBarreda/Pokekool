using FluentValidation;

namespace Pokekool.Application.Collection.Commands.SetCardPriority;

public sealed class SetCardPriorityCommandValidator : AbstractValidator<SetCardPriorityCommand>
{
    public SetCardPriorityCommandValidator()
    {
        RuleFor(c => c.CardId).NotEmpty();
    }
}
