using CCE.Api.Common.Extensions;
using CCE.Application.InteractiveMaps.Public.Queries.GetInteractiveMapById;
using CCE.Application.InteractiveMaps.Public.Queries.GetInteractiveMapNodeDetails;
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

        maps.MapGet("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetPublicInteractiveMapByIdQuery(id), cancellationToken).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("GetPublicInteractiveMapById");

        // GET /api/interactive-maps/nodes/{nodeId}/details
        // Returns the side-panel details when a user clicks a map node:
        // node info + linked topic + top-5 news, events, posts, and resources.
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
