using CCE.Application.Content.Queries.GetNewsById;
using CCE.Application.Content.Queries.ListNews;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class NewsEndpoints
{
    public static IEndpointRouteBuilder MapNewsEndpoints(this IEndpointRouteBuilder app)
    {
        var news = app.MapGroup("/api/admin/news").WithTags("News");

        news.MapGet("", async (
            int? page, int? pageSize, string? search, bool? isPublished, bool? isFeatured,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var query = new ListNewsQuery(
                Page: page ?? 1,
                PageSize: pageSize ?? 20,
                Search: search,
                IsPublished: isPublished,
                IsFeatured: isFeatured);
            var result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.News_Update)
        .WithName("ListNews");

        news.MapGet("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var dto = await mediator.Send(new GetNewsByIdQuery(id), cancellationToken).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .RequireAuthorization(Permissions.News_Update)
        .WithName("GetNewsById");

        return app;
    }
}
