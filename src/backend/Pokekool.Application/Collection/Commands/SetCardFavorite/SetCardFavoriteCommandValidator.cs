using FluentValidation;

namespace Pokekool.Application.Collection.Commands.SetCardFavorite;

public sealed class SetCardFavoriteCommandValidator : AbstractValidator<SetCardFavoriteCommand>
{
    public SetCardFavoriteCommandValidator()
    {
        RuleFor(c => c.CardId).NotEmpty();
    }
}
