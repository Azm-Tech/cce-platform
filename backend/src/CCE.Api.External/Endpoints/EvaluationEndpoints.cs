using CCE.Api.Common.Extensions;
using CCE.Application.Evaluation.Commands.SubmitEvaluation;
using CCE.Domain.Evaluation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class EvaluationEndpoints
{
    public static IEndpointRouteBuilder MapEvaluationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/evaluations").WithTags("Evaluations");

        // POST /api/evaluations — public submit (visitors & authenticated users)
        group.MapPost("", async (
            SubmitEvaluationRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            
            var cmd = new SubmitEvaluationCommand(
                (EvaluationRating)body.OverallSatisfaction,
                (EvaluationRating)body.EaseOfUse,
                (EvaluationRating)body.ContentSuitability,
                body.Feedback);
            var result = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return result.ToCreatedHttpResult();
        })
        .AllowAnonymous()
        .WithName("SubmitEvaluation");

        return app;
    }
}

public sealed record SubmitEvaluationRequest(
    int OverallSatisfaction,
    int EaseOfUse,
    int ContentSuitability,
    string Feedback);
