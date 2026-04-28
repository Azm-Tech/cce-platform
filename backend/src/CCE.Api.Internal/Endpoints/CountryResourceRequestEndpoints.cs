using CCE.Application.Content.Commands.ApproveCountryResourceRequest;
using CCE.Application.Content.Commands.RejectCountryResourceRequest;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class CountryResourceRequestEndpoints
{
    public static IEndpointRouteBuilder MapCountryResourceRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var requests = app.MapGroup("/api/admin/country-resource-requests").WithTags("CountryResourceRequests");

        requests.MapPost("/{id:guid}/approve", async (
            System.Guid id,
            ApproveCountryResourceRequestRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new ApproveCountryResourceRequestCommand(id, body.AdminNotesAr, body.AdminNotesEn);
            var dto = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return Results.Ok(dto);
        })
        .RequireAuthorization(Permissions.Resource_Country_Approve)
        .WithName("ApproveCountryResourceRequest");

        requests.MapPost("/{id:guid}/reject", async (
            System.Guid id,
            RejectCountryResourceRequestRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new RejectCountryResourceRequestCommand(id, body.AdminNotesAr, body.AdminNotesEn);
            var dto = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return Results.Ok(dto);
        })
        .RequireAuthorization(Permissions.Resource_Country_Reject)
        .WithName("RejectCountryResourceRequest");

        return app;
    }
}

public sealed record ApproveCountryResourceRequestRequest(string? AdminNotesAr, string? AdminNotesEn);
public sealed record RejectCountryResourceRequestRequest(string AdminNotesAr, string AdminNotesEn);
