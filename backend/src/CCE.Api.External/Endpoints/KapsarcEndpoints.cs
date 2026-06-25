using CCE.Api.Common.Extensions;
using CCE.Application.Kapsarc.Queries.GetLatestKapsarcSnapshot;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class KapsarcEndpoints
{
    public static IEndpointRouteBuilder MapKapsarcEndpoints(this IEndpointRouteBuilder app)
    {
        var kapsarc = app.MapGroup("/api/kapsarc").WithTags("Kapsarc");

        // GET /api/kapsarc/snapshots/{countryId}
        kapsarc.MapGet("/snapshots/{countryId:guid}", async (
            System.Guid countryId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetLatestKapsarcSnapshotQuery(countryId), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .AllowAnonymous()
        .WithName("GetLatestKapsarcSnapshot");

        return app;
    }
}
