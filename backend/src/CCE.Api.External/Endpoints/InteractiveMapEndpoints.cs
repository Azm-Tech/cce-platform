using CCE.Api.Common.Extensions;
using CCE.Application.InteractiveMaps.Public.Queries.GetCurrentInteractiveMap;
using CCE.Application.InteractiveMaps.Public.Queries.GetInteractiveMapNodeDetails;
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
            var result = await mediator.Send(new GetCurrentInteractiveMapQuery(), cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("GetCurrentInteractiveMap");

        // GET /api/interactive-maps/nodes/{nodeId}/details
        // Returns the side-panel details when a user clicks a map node:
        // node info + top-5 news, events, and resources.
        maps.MapGet("/nodes/{nodeId:guid}/details", async (
            System.Guid nodeId,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetInteractiveMapNodeDetailsQuery(nodeId), cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("GetInteractiveMapNodeDetails");

        return app;
    }
}
