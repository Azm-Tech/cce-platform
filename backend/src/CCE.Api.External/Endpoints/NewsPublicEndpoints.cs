using CCE.Api.Common.Extensions;
using CCE.Application.Content.Public.Queries.GetPublicNewsBySlug;
using CCE.Application.Content.Public.Queries.ListPublicNews;
using CCE.Application.Content.Public.Queries.GetPublicNewsById;
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
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var query = new ListPublicNewsQuery(
                Page: page ?? 1,
                PageSize: pageSize ?? 20,
                IsFeatured: isFeatured,
                TopicSlug: topicSlug);
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

        news.MapGet("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(new GetPublicNewsByIdQuery(id), cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("GetPublicNewsById");

        return app;
    }
}
