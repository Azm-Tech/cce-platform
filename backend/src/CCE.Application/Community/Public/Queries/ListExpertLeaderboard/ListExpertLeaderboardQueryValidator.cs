using FluentValidation;

namespace CCE.Application.Community.Public.Queries.ListExpertLeaderboard;

public sealed class ListExpertLeaderboardQueryValidator : AbstractValidator<ListExpertLeaderboardQuery>
{
    public ListExpertLeaderboardQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
