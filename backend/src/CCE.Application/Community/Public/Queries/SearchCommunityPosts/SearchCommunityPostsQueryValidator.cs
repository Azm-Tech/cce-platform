using FluentValidation;

namespace CCE.Application.Community.Public.Queries.SearchCommunityPosts;

public sealed class SearchCommunityPostsQueryValidator : AbstractValidator<SearchCommunityPostsQuery>
{
    public SearchCommunityPostsQueryValidator()
    {
        RuleFor(x => x.SearchTerm).NotEmpty().MinimumLength(2).MaximumLength(200);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.TagIds).Must(t => t is null || t.Count <= 20)
            .WithMessage("At most 20 tag IDs may be supplied.");
    }
}
