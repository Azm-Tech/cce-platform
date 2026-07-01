using CCE.Api.Common.Extensions;
using CCE.Application.InteractiveMaps.Commands.CreateInteractiveMapNode;
using CCE.Application.InteractiveMaps.Commands.DeleteInteractiveMapNode;
using CCE.Application.InteractiveMaps.Commands.UpdateInteractiveMap;
using CCE.Application.InteractiveMaps.Commands.UpdateInteractiveMapNode;
using CCE.Application.InteractiveMaps.Queries.GetCurrentInteractiveMap;
using CCE.Application.InteractiveMaps.Queries.ListInteractiveMapNodes;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class InteractiveMapEndpoints
{
    public static IEndpointRouteBuilder MapInteractiveMapEndpoints(this IEndpointRouteBuilder app)
    {
        var maps = app.MapGroup("/api/admin/interactive-maps").WithTags("InteractiveMaps");

        maps.MapGet("", async (
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(new GetCurrentInteractiveMapQuery(), cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.InteractiveMap_Manage)
        .WithName("GetCurrentInteractiveMap");

        maps.MapPut("", async (
            UpdateInteractiveMapRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new UpdateInteractiveMapCommand(
                body.NameAr, body.NameEn, body.DescriptionAr, body.DescriptionEn);
            var response = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.InteractiveMap_Manage)
        .WithName("UpdateInteractiveMap");

        // ─── Nodes ───

        maps.MapGet("/{mapId:guid}/nodes", async (
            System.Guid mapId, int? page, int? pageSize, bool? isActive,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var query = new ListInteractiveMapNodesQuery(
                MapId: mapId,
                Page: page ?? 1,
                PageSize: pageSize ?? 20,
                IsActive: isActive);
            var response = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.InteractiveMap_Manage)
        .WithName("ListInteractiveMapNodes");

        maps.MapPost("/{mapId:guid}/nodes", async (
            System.Guid mapId,
            CreateInteractiveMapNodeRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new CreateInteractiveMapNodeCommand(
                mapId, body.NameAr, body.NameEn, body.IconKey, body.Category,
                body.CategoryNameAr, body.CategoryNameEn,
                body.TitleAr, body.TitleEn, body.DescriptionAr, body.DescriptionEn,
                body.ParentId, body.TopicId);
            var response = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return response.ToCreatedHttpResult();
        })
        .RequireAuthorization(Permissions.InteractiveMap_Manage)
        .WithName("CreateInteractiveMapNode");

        maps.MapPut("/{mapId:guid}/nodes/{id:guid}", async (
            System.Guid mapId, System.Guid id,
            UpdateInteractiveMapNodeRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new UpdateInteractiveMapNodeCommand(
                mapId, id, body.NameAr, body.NameEn, body.IconKey, body.Category,
                body.CategoryNameAr, body.CategoryNameEn,
                body.TitleAr, body.TitleEn, body.DescriptionAr, body.DescriptionEn,
                body.ParentId, body.TopicId, body.IsActive);
            var response = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.InteractiveMap_Manage)
        .WithName("UpdateInteractiveMapNode");

        maps.MapDelete("/{mapId:guid}/nodes/{id:guid}", async (
            System.Guid mapId, System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(new DeleteInteractiveMapNodeCommand(mapId, id), cancellationToken).ConfigureAwait(false);
            return response.ToNoContentHttpResult();
        })
        .RequireAuthorization(Permissions.InteractiveMap_Manage)
        .WithName("DeleteInteractiveMapNode");

        return app;
    }
}

public sealed record UpdateInteractiveMapRequest(
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn);

public sealed record CreateInteractiveMapNodeRequest(
    string NameAr,
    string NameEn,
    string IconKey,
    int? Category,
    string? CategoryNameAr,
    string? CategoryNameEn,
    string? TitleAr,
    string? TitleEn,
    string? DescriptionAr,
    string? DescriptionEn,
    System.Guid? ParentId,
    System.Guid TopicId);

public sealed record UpdateInteractiveMapNodeRequest(
    string NameAr,
    string NameEn,
    string IconKey,
    int? Category,
    string? CategoryNameAr,
    string? CategoryNameEn,
    string? TitleAr,
    string? TitleEn,
    string? DescriptionAr,
    string? DescriptionEn,
    System.Guid? ParentId,
    System.Guid TopicId,
    bool IsActive);
