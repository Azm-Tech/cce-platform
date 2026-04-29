using CCE.Application.Content.Public.Queries.GetPublicPageBySlug;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class PagesPublicEndpoints
{
    public static IEndpointRouteBuilder MapPagesPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var pages = app.MapGroup("/api/pages").WithTags("PagesPublic");

        pages.MapGet("/{slug}", async (string slug, IMediator mediator, CancellationToken ct) =>
        {
            var dto = await mediator.Send(new GetPublicPageBySlugQuery(slug), ct).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .AllowAnonymous()
        .WithName("GetPublicPageBySlug");

        return app;
    }
}
