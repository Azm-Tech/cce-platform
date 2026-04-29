using CCE.Application.Content.Commands.CreateNews;
using CCE.Application.Content.Commands.DeleteNews;
using CCE.Application.Content.Commands.PublishNews;
using CCE.Application.Content.Commands.UpdateNews;
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

        news.MapPost("", async (
            CreateNewsRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new CreateNewsCommand(body.TitleAr, body.TitleEn, body.ContentAr, body.ContentEn, body.Slug, body.FeaturedImageUrl);
            var dto = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return Results.Created($"/api/admin/news/{dto.Id}", dto);
        })
        .RequireAuthorization(Permissions.News_Update)
        .WithName("CreateNews");

        news.MapPut("/{id:guid}", async (
            System.Guid id,
            UpdateNewsRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var rowVersion = string.IsNullOrEmpty(body.RowVersion) ? System.Array.Empty<byte>() : System.Convert.FromBase64String(body.RowVersion);
            var cmd = new UpdateNewsCommand(id, body.TitleAr, body.TitleEn, body.ContentAr, body.ContentEn, body.Slug, body.FeaturedImageUrl, rowVersion);
            var dto = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .RequireAuthorization(Permissions.News_Update)
        .WithName("UpdateNews");

        news.MapDelete("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            await mediator.Send(new DeleteNewsCommand(id), cancellationToken).ConfigureAwait(false);
            return Results.NoContent();
        })
        .RequireAuthorization(Permissions.News_Delete)
        .WithName("DeleteNews");

        news.MapPost("/{id:guid}/publish", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var dto = await mediator.Send(new PublishNewsCommand(id), cancellationToken).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .RequireAuthorization(Permissions.News_Publish)
        .WithName("PublishNews");

        return app;
    }
}

public sealed record CreateNewsRequest(
    string TitleAr, string TitleEn, string ContentAr, string ContentEn,
    string Slug, string? FeaturedImageUrl);

public sealed record UpdateNewsRequest(
    string TitleAr, string TitleEn, string ContentAr, string ContentEn,
    string Slug, string? FeaturedImageUrl, string RowVersion);
