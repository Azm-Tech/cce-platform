using CCE.Api.Common.Extensions;
using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Public.Commands.UserInterest;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class UserInterestEndpoints
{
    public static IEndpointRouteBuilder MapUserInterestEndpoints(this IEndpointRouteBuilder app)
    {
        var me = app.MapGroup("/api/me").WithTags("User Interests").RequireAuthorization();

        me.MapPatch("/interests", async (
            UpsertUserInterestRequest body,
            ICurrentUserAccessor currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return Results.Unauthorized();

            var result = await mediator.Send(
                new UpsertUserInterestCommand(userId, body.Interests), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .WithName("UpsertUserInterest");

        return app;
    }
}

public sealed record UpsertUserInterestRequest(IReadOnlyList<string> Interests);
