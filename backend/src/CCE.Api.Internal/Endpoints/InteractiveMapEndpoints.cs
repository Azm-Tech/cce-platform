using CCE.Api.Common.Extensions;
using CCE.Application.InteractiveMaps.Commands.CreateInteractiveMap;
using CCE.Application.InteractiveMaps.Commands.CreateInteractiveMapNode;
using CCE.Application.InteractiveMaps.Commands.DeleteInteractiveMap;
using CCE.Application.InteractiveMaps.Commands.DeleteInteractiveMapNode;
using CCE.Application.InteractiveMaps.Commands.UpdateInteractiveMap;
using CCE.Application.InteractiveMaps.Commands.UpdateInteractiveMapNode;
using CCE.Application.InteractiveMaps.Queries.GetInteractiveMapById;
using CCE.Application.InteractiveMaps.Queries.ListInteractiveMapNodes;
using CCE.Application.InteractiveMaps.Queries.ListInteractiveMaps;
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
            int? page, int? pageSize, bool? isActive,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var query = new ListInteractiveMapsQuery(
                Page: page ?? 1,
                PageSize: pageSize ?? 20,
                IsActive: isActive);
            var response = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.InteractiveMap_Manage)
        .WithName("ListInteractiveMaps");

        maps.MapGet("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(new GetInteractiveMapByIdQuery(id), cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.InteractiveMap_Manage)
        .WithName("GetInteractiveMapById");

        maps.MapPost("", async (
            CreateInteractiveMapRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new CreateInteractiveMapCommand(
                body.NameAr, body.NameEn, body.DescriptionAr, body.DescriptionEn, body.Slug);
            var response = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return response.ToCreatedHttpResult();
        })
        .RequireAuthorization(Permissions.InteractiveMap_Manage)
        .WithName("CreateInteractiveMap");

        maps.MapPut("/{id:guid}", async (
            System.Guid id,
            UpdateInteractiveMapRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new UpdateInteractiveMapCommand(
                id, body.NameAr, body.NameEn, body.DescriptionAr, body.DescriptionEn, body.Slug, body.IsActive);
            var response = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.InteractiveMap_Manage)
        .WithName("UpdateInteractiveMap");

        maps.MapDelete("/{id:guid}", async (
            System.Guid id,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(new DeleteInteractiveMapCommand(id), cancellationToken).ConfigureAwait(false);
            return response.ToNoContentHttpResult();
        })
        .RequireAuthorization(Permissions.InteractiveMap_Manage)
        .WithName("DeleteInteractiveMap");

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
                body.CategoryNameAr, body.CategoryNameEn, body.Level,
                body.ParentId, body.TopicId, body.TopicSlug);
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
                body.CategoryNameAr, body.CategoryNameEn, body.Level,
                body.ParentId, body.TopicId, body.TopicSlug, body.IsActive);
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

public sealed record CreateInteractiveMapRequest(
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    string Slug);

public sealed record UpdateInteractiveMapRequest(
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    string Slug,
    bool IsActive);

public sealed record CreateInteractiveMapNodeRequest(
    string NameAr,
    string NameEn,
    string IconKey,
    int? Category,
    string? CategoryNameAr,
    string? CategoryNameEn,
    int Level,
    System.Guid? ParentId,
    System.Guid? TopicId,
    string? TopicSlug);

public sealed record UpdateInteractiveMapNodeRequest(
    string NameAr,
    string NameEn,
    string IconKey,
    int? Category,
    string? CategoryNameAr,
    string? CategoryNameEn,
    int Level,
    System.Guid? ParentId,
    System.Guid? TopicId,
    string? TopicSlug,
    bool IsActive);
