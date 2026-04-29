using CCE.Application.Content.Commands.CreatePage;
using CCE.Application.Content.Queries.GetPageById;
using CCE.Application.Content.Queries.ListPages;
using CCE.Domain;
using CCE.Domain.Content;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class PageEndpoints
{
    public static IEndpointRouteBuilder MapPageEndpoints(this IEndpointRouteBuilder app)
    {
        var pages = app.MapGroup("/api/admin/pages").WithTags("Pages");

        pages.MapGet("", async (
            int? page, int? pageSize, string? search, PageType? pageType,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var query = new ListPagesQuery(page ?? 1, pageSize ?? 20, search, pageType);
            var result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.Page_Edit)
        .WithName("ListPages");

        pages.MapGet("/{id:guid}", async (System.Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var dto = await mediator.Send(new GetPageByIdQuery(id), cancellationToken).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .RequireAuthorization(Permissions.Page_Edit)
        .WithName("GetPageById");

        pages.MapPost("", async (CreatePageRequest body, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new CreatePageCommand(body.Slug, body.PageType, body.TitleAr, body.TitleEn, body.ContentAr, body.ContentEn);
            var dto = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return Results.Created($"/api/admin/pages/{dto.Id}", dto);
        })
        .RequireAuthorization(Permissions.Page_Edit)
        .WithName("CreatePage");

        return app;
    }
}

public sealed record CreatePageRequest(
    string Slug,
    CCE.Domain.Content.PageType PageType,
    string TitleAr, string TitleEn,
    string ContentAr, string ContentEn);
