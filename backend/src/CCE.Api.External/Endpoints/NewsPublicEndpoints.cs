using CCE.Api.Common.Extensions;
using CCE.Application.Common;
using CCE.Application.Content.Public.Queries.GetPublicNewsBySlug;
using CCE.Application.Content.Public.Queries.ListPublicNews;
using CCE.Domain.Content;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class NewsPublicEndpoints
{
    public static IEndpointRouteBuilder MapNewsPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var news = app.MapGroup("/api/news").WithTags("News");

        news.MapGet("", async (
            int? page, int? pageSize, bool? isFeatured, string? topicSlug,
            NewsSortBy? sortBy, SortOrder? sortOrder,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var query = new ListPublicNewsQuery(
                Page: page ?? 1,
                PageSize: pageSize ?? 20,
                IsFeatured: isFeatured,
                TopicSlug: topicSlug,
                SortBy: sortBy ?? NewsSortBy.Date,
                SortOrder: sortOrder ?? SortOrder.Descending);
            var response = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("ListPublicNews");

        news.MapGet("/{slug}", async (
            string slug,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(new GetPublicNewsBySlugQuery(slug), cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("GetPublicNewsBySlug");

        return app;
    }
}
