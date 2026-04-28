using CCE.Application.Identity.Queries.ListExpertRequests;
using CCE.Domain;
using CCE.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class ExpertEndpoints
{
    public static IEndpointRouteBuilder MapExpertEndpoints(this IEndpointRouteBuilder app)
    {
        var requests = app.MapGroup("/api/admin/expert-requests").WithTags("Experts");

        requests.MapGet("", async (
            int? page, int? pageSize, ExpertRegistrationStatus? status, System.Guid? requestedById,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var query = new ListExpertRequestsQuery(
                Page: page ?? 1,
                PageSize: pageSize ?? 20,
                Status: status,
                RequestedById: requestedById);
            var result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.Community_Expert_ApproveRequest)
        .WithName("ListExpertRequests");

        return app;
    }
}
