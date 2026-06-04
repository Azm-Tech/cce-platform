using CCE.Api.Common.Extensions;
using CCE.Application.Content.Public.Queries.ListHomepageFeed;
using CCE.Domain.Common;
using CCE.Domain.Content;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class HomepageFeedPublicEndpoints
{
    public static IEndpointRouteBuilder MapHomepageFeedPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var feed = app.MapGroup("/api/feed/news-events").WithTags("Feed");

        feed.MapGet("", async (
            int? page,
            int? pageSize,
            HomepageFeedContentType? type,
            System.Guid? topicId,
            HomepageFeedSortBy? sortBy,
            SortOrder? sortOrder,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new ListHomepageFeedQuery(
                Page: page ?? 1,
                PageSize: pageSize ?? 20,
                ContentType: type,
                TopicId: topicId,
                SortBy: sortBy ?? HomepageFeedSortBy.Date,
                SortOrder: sortOrder ?? SortOrder.Descending);
            var response = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("ListHomepageFeed");

        return app;
    }
}
