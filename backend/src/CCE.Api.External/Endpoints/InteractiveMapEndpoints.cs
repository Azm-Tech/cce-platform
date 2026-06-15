using CCE.Api.Common.Extensions;
using CCE.Application.InteractiveMaps.Public.Queries.GetInteractiveMapBySlug;
using CCE.Application.InteractiveMaps.Public.Queries.ListInteractiveMaps;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class InteractiveMapEndpoints
{
    public static IEndpointRouteBuilder MapInteractiveMapPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var maps = app.MapGroup("/api/interactive-maps").WithTags("InteractiveMaps");

        maps.MapGet("", async (
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new ListInteractiveMapsQuery(), cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("ListPublicInteractiveMaps");

        maps.MapGet("/{slug}", async (
            string slug,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetInteractiveMapBySlugQuery(slug), cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("GetPublicInteractiveMapBySlug");

        return app;
    }
}
