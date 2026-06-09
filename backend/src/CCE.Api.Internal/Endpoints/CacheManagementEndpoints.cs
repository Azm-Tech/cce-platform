using CCE.Api.Common.Extensions;
using CCE.Application.Cache;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

/// <summary>
/// Admin cache-management endpoints. Lets operators inspect the output-cache "tables" (regions) and
/// reload/delete them by key. Reload = purge → the next public request rebuilds the entries.
/// </summary>
public static class CacheManagementEndpoints
{
    public static IEndpointRouteBuilder MapCacheManagementEndpoints(this IEndpointRouteBuilder app)
    {
        var cache = app.MapGroup("/api/admin/cache").WithTags("Cache");

        // List regions ("tables") + entry counts.
        cache.MapGet("/regions", async (IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(new GetCacheRegionsQuery(), cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Cache_Manage)
        .WithName("ListCacheRegions");

        // Reload a region (purge; lazy repopulate on next read).
        cache.MapPost("/regions/{region}/reload", async (
            string region, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(new EvictCacheRegionCommand(region), cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Cache_Manage)
        .WithName("ReloadCacheRegion");

        // Delete a region (same purge; delete semantics).
        cache.MapDelete("/regions/{region}", async (
            string region, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(new EvictCacheRegionCommand(region), cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Cache_Manage)
        .WithName("DeleteCacheRegion");

        // Delete a single key, e.g. ?key=out:/api/resources?page=1|lang=en
        cache.MapDelete("/keys", async (
            string key, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(new EvictCacheKeyCommand(key), cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Cache_Manage)
        .WithName("DeleteCacheKey");

        // Flush every region.
        cache.MapPost("/flush", async (IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator.Send(new FlushCacheCommand(), cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Cache_Manage)
        .WithName("FlushCache");

        return app;
    }
}
