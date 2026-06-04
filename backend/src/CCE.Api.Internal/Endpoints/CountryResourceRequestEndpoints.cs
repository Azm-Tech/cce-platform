using CCE.Api.Common.Extensions;
using CCE.Application.Content.Commands.ApproveCountryResourceRequest;
using CCE.Application.Content.Commands.RejectCountryResourceRequest;
using CCE.Application.Content.Queries.GetCountryContentRequest;
using CCE.Application.Content.Queries.ListCountryContentRequests;
using CCE.Domain;
using CCE.Domain.Content;
using CCE.Domain.Country;
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

        // US049 — list all country content requests (admin sees all; no scope filter)
        requests.MapGet("", async (
            int? page, int? pageSize,
            CountryContentRequestStatus? status, ContentType? type, System.Guid? countryId,
            IMediator mediator, CancellationToken cancellationToken) =>
            (await mediator.Send(
                new ListCountryContentRequestsQuery(page ?? 1, pageSize ?? 20, status, type, countryId),
                cancellationToken).ConfigureAwait(false)).ToHttpResult())
        .RequireAuthorization(Permissions.Resource_Country_Approve)
        .WithName("ListAdminCountryContentRequests");

        // US049 — single request detail
        requests.MapGet("/{id:guid}", async (
            System.Guid id, IMediator mediator, CancellationToken cancellationToken) =>
            (await mediator.Send(new GetCountryContentRequestQuery(id), cancellationToken)
                .ConfigureAwait(false)).ToHttpResult())
        .RequireAuthorization(Permissions.Resource_Country_Approve)
        .WithName("GetAdminCountryContentRequest");

        // US050 — approve (CON023 on success, ERR031 on state violation)
        requests.MapPost("/{id:guid}/approve", async (
            System.Guid id,
            ApproveCountryResourceRequestRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new ApproveCountryResourceRequestCommand(id, body.AdminNotesAr, body.AdminNotesEn);
            var response = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Resource_Country_Approve)
        .WithName("ApproveCountryResourceRequest");

        // US050 — reject (CON023 on success, ERR031 on state violation)
        requests.MapPost("/{id:guid}/reject", async (
            System.Guid id,
            RejectCountryResourceRequestRequest body,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var cmd = new RejectCountryResourceRequestCommand(id, body.AdminNotesAr, body.AdminNotesEn);
            var response = await mediator.Send(cmd, cancellationToken).ConfigureAwait(false);
            return response.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Resource_Country_Reject)
        .WithName("RejectCountryResourceRequest");

        return app;
    }
}
