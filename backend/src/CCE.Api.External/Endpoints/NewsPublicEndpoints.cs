using CCE.Application.Content.Public.Queries.GetPublicNewsBySlug;
using CCE.Application.Content.Public.Queries.ListPublicNews;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class NewsPublicEndpoints
{
    public static IEndpointRouteBuilder MapNewsPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var news = app.MapGroup("/api/news").WithTags("News");

        news.MapGet("", async (
            int? page, int? pageSize, bool? isFeatured,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var query = new ListPublicNewsQuery(
                Page: page ?? 1,
                PageSize: pageSize ?? 20,
                IsFeatured: isFeatured);
            var result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .WithName("ListPublicNews");

        news.MapGet("/{slug}", async (
            string slug,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var dto = await mediator.Send(new GetPublicNewsBySlugQuery(slug), cancellationToken).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .AllowAnonymous()
        .WithName("GetPublicNewsBySlug");

        return app;
    }
}
