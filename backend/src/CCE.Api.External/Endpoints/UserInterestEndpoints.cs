using CCE.Api.Common.Extensions;
using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Public.Commands.UserInterest;
using CCE.Application.Identity.Public.Dtos;
using CCE.Application.Identity.Public.Queries.GetMyInterests;
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

        me.MapGet("/interests", async (
            ICurrentUserAccessor currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return Results.Unauthorized();

            var result = await mediator.Send(new GetMyInterestsQuery(userId), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .WithName("GetMyInterests");

        me.MapPatch("/interests", async (
            UpsertUserInterestRequest body,
            ICurrentUserAccessor currentUser,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = currentUser.GetUserId() ?? System.Guid.Empty;
            if (userId == System.Guid.Empty) return Results.Unauthorized();

            var result = await mediator.Send(
                new UpsertUserInterestCommand(
                    userId,
                    body.CarbonAreaIds,
                    body.KnowledgeAssessmentId,
                    body.JobSectorId,
                    body.TargetCountryId), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .WithName("UpsertUserInterest");

        return app;
    }
}

public sealed record UpsertUserInterestRequest(
    IReadOnlyList<System.Guid>? CarbonAreaIds,
    System.Guid? KnowledgeAssessmentId,
    System.Guid? JobSectorId,
    System.Guid? TargetCountryId);
