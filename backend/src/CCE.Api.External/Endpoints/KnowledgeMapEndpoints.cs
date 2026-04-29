using CCE.Application.KnowledgeMaps.Public.Queries.GetKnowledgeMapById;
using CCE.Application.KnowledgeMaps.Public.Queries.ListKnowledgeMapEdges;
using CCE.Application.KnowledgeMaps.Public.Queries.ListKnowledgeMapNodes;
using CCE.Application.KnowledgeMaps.Public.Queries.ListKnowledgeMaps;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class KnowledgeMapEndpoints
{
    public static IEndpointRouteBuilder MapKnowledgeMapEndpoints(this IEndpointRouteBuilder app)
    {
        var maps = app.MapGroup("/api/knowledge-maps").WithTags("KnowledgeMap");

        maps.MapGet("", async (
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new ListKnowledgeMapsQuery(), cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .WithName("ListKnowledgeMaps");

        maps.MapGet("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var dto = await mediator.Send(new GetKnowledgeMapByIdQuery(id), cancellationToken).ConfigureAwait(false);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .AllowAnonymous()
        .WithName("GetKnowledgeMapById");

        maps.MapGet("/{id:guid}/nodes", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new ListKnowledgeMapNodesQuery(id), cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .WithName("ListKnowledgeMapNodes");

        maps.MapGet("/{id:guid}/edges", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new ListKnowledgeMapEdgesQuery(id), cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .WithName("ListKnowledgeMapEdges");

        return app;
    }
}
