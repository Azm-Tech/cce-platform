using CCE.Api.Common.Extensions;
using CCE.Application.Community.Public.Queries.ListFeaturedPosts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class FeaturedPostsFeedEndpoints
{
    public static IEndpointRouteBuilder MapFeaturedPostsFeedEndpoints(this IEndpointRouteBuilder app)
    {
        var feed = app.MapGroup("/api/feed/featured-posts").WithTags("Feed");

        // Paged list of the most popular community posts (default 10 per page)
        feed.MapGet("", async (
            int? page, int? pageSize, System.Guid? topicId,
            IMediator mediator, CancellationToken cancellationToken) =>
            (await mediator.Send(
                new ListFeaturedPostsQuery(page ?? 1, pageSize ?? 10, topicId),
                cancellationToken).ConfigureAwait(false)).ToHttpResult())
        .AllowAnonymous()
        .WithName("ListFeaturedPosts");

        return app;
    }
}
