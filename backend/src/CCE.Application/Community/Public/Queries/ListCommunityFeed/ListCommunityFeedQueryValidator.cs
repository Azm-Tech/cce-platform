using FluentValidation;

namespace CCE.Application.Community.Public.Queries.ListCommunityFeed;

public sealed class ListCommunityFeedQueryValidator : AbstractValidator<ListCommunityFeedQuery>
{
    public ListCommunityFeedQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.TagIds).Must(t => t is null || t.Count <= 20)
            .WithMessage("At most 20 tag IDs may be supplied.");
    }
}
