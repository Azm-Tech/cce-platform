using CCE.Api.Common.Extensions;
using CCE.Application.Evaluation.Queries.GetAllEvaluations;
using CCE.Application.Evaluation.Queries.GetEvaluationById;
using CCE.Domain;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.Internal.Endpoints;

public static class EvaluationEndpoints
{
    public static IEndpointRouteBuilder MapEvaluationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/evaluations").WithTags("Evaluations");

        // GET /api/admin/evaluations — list all (admin only)
        group.MapGet("", async (
            int? page, int? pageSize,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetAllEvaluationsQuery(page ?? 1, pageSize ?? 20), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Survey_ReadAll)
        .WithName("GetAllEvaluations");

        // GET /api/admin/evaluations/{id} — get by id (admin only)
        group.MapGet("{id:guid}", async (
            System.Guid id,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetEvaluationByIdQuery(id), ct).ConfigureAwait(false);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.Survey_ReadAll)
        .WithName("GetEvaluationById");

        return app;
    }
}
