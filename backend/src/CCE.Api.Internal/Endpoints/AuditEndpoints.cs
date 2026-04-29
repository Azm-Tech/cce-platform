using CCE.Application.Audit.Queries.ListAuditEvents;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/audit-events").WithTags("Audit");

        group.MapGet("", async (
            int? page, int? pageSize,
            string? actor, string? actionPrefix, string? resourceType,
            System.Guid? correlationId,
            System.DateTimeOffset? from, System.DateTimeOffset? to,
            IMediator mediator, CancellationToken cancellationToken) =>
        {
            var query = new ListAuditEventsQuery(
                page ?? 1, pageSize ?? 50,
                actor, actionPrefix, resourceType,
                correlationId, from, to);
            var result = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .RequireAuthorization(Permissions.Audit_Read)
        .WithName("ListAuditEvents");

        return app;
    }
}
