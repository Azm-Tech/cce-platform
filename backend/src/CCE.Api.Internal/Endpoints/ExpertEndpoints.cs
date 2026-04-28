using CCE.Application.Identity.Commands.ApproveExpertRequest;
using CCE.Application.Identity.Commands.RejectExpertRequest;
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

        requests.MapPost("/{id:guid}/approve", async (
            System.Guid id,
            ApproveExpertRequestRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new ApproveExpertRequestCommand(id, body.AcademicTitleAr, body.AcademicTitleEn);
            var dto = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return Results.Ok(dto);
        })
        .RequireAuthorization(Permissions.Community_Expert_ApproveRequest)
        .WithName("ApproveExpertRequest");

        requests.MapPost("/{id:guid}/reject", async (
            System.Guid id,
            RejectExpertRequestRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new RejectExpertRequestCommand(id, body.RejectionReasonAr, body.RejectionReasonEn);
            var dto = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return Results.Ok(dto);
        })
        .RequireAuthorization(Permissions.Community_Expert_ApproveRequest)
        .WithName("RejectExpertRequest");

        return app;
    }
}

public sealed record ApproveExpertRequestRequest(string AcademicTitleAr, string AcademicTitleEn);
public sealed record RejectExpertRequestRequest(string RejectionReasonAr, string RejectionReasonEn);
