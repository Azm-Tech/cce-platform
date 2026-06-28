using CCE.Api.Common.Extensions;
using CCE.Application.Kapsarc.Commands.RefreshKapsarcSnapshot;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class KapsarcAdminEndpoints
{
    public static IEndpointRouteBuilder MapKapsarcAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/countries/{countryId:guid}/kapsarc").WithTags("Kapsarc");

        // US014 / BRD §6.5.1 — pull latest CCE data from KAPSARC and capture a new snapshot
        group.MapPost("/refresh", async (
            System.Guid countryId, IMediator mediator, CancellationToken cancellationToken) =>
            (await mediator.Send(new RefreshKapsarcSnapshotCommand(countryId), cancellationToken)
                .ConfigureAwait(false)).ToHttpResult())
        .RequireAuthorization(Permissions.Country_Kapsarc_Refresh)
        .WithName("RefreshKapsarcSnapshot");

        return app;
    }
}
