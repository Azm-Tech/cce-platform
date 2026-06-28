using CCE.Api.Common.Extensions;
using CCE.Application.Cache;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

/// <summary>
/// Redis diagnostics endpoint for the External API. Lets operators inspect the raw Redis keyspace
/// (localhost:6379) where the output cache lives.
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
        .WithName("ListRedisKeysExternal");

        return app;
    }
}
