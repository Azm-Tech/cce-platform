using CCE.Api.Common.Extensions;
using CCE.Application.Cache;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

/// <summary>
/// Admin Redis diagnostics endpoints. Lets operators inspect the raw Redis keyspace for
/// troubleshooting the output cache, SignalR backplane, or any other Redis usage.
/// </summary>
public static class RedisAdminEndpoints
{
    public static IEndpointRouteBuilder MapRedisAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var redis = app.MapGroup("/api/admin/redis").WithTags("Redis");

        // GET /api/admin/redis/keys?pattern=*&count=100
        redis.MapGet("/keys", async (
            string? pattern,
            int? count,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new ListRedisKeysQuery(
                Pattern: pattern ?? "*",
                Count: count ?? 100);
            var response = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Cache_Manage)
        .WithName("ListRedisKeys");

        return app;
    }
}
