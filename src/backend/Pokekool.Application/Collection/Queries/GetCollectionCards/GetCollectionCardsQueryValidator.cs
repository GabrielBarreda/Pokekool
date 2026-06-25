using FluentValidation;

namespace Pokekool.Application.Collection.Queries.GetCollectionCards;

public sealed class GetCollectionCardsQueryValidator : AbstractValidator<GetCollectionCardsQuery>
{
    private static readonly HashSet<string> ValidSortBy =
        ["name_asc", "name_desc", "rarity", "number"];

    public GetCollectionCardsQueryValidator()
    {
        RuleFor(q => q.Page).GreaterThan(0);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 200);
        RuleFor(q => q.SortBy)
            .Must(s => s == null || ValidSortBy.Contains(s))
            .WithMessage($"SortBy must be one of: {string.Join(", ", ValidSortBy)}");
    }
}
