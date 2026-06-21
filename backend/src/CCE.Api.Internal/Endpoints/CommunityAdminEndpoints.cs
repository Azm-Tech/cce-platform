using CCE.Api.Common.Extensions;
using CCE.Application.Community.Commands.RebuildHotLeaderboard;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

/// <summary>
/// Admin endpoints for community feed maintenance. Offline repair only — not triggered by runtime events.
/// </summary>
public static class CommunityAdminEndpoints
{
    public static IEndpointRouteBuilder MapCommunityAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/community").WithTags("Community Admin");

        // Rebuild hot leaderboard for a single community from SQL scores.
        group.MapPost("/{communityId:guid}/hot-leaderboard/rebuild", async (
            Guid communityId, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator
                .Send(new RebuildHotLeaderboardCommand(communityId), cancellationToken)
                .ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Cache_Manage)
        .WithName("RebuildCommunityHotLeaderboard");

        // Rebuild hot leaderboards for ALL communities at once (full recovery).
        group.MapPost("/hot-leaderboard/rebuild-all", async (
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var response = await mediator
                .Send(new RebuildHotLeaderboardCommand(null), cancellationToken)
                .ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Cache_Manage)
        .WithName("RebuildAllHotLeaderboards");

        return app;
    }
}
