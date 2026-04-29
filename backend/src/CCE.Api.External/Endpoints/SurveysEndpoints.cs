using CCE.Application.Surveys.Commands.SubmitServiceRating;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class SurveysEndpoints
{
    public static IEndpointRouteBuilder MapSurveysEndpoints(this IEndpointRouteBuilder app)
    {
        var surveys = app.MapGroup("/api/surveys").WithTags("Surveys");

        // POST /api/surveys/service-rating
        surveys.MapPost("/service-rating", async (
            ServiceRatingRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var cmd = new SubmitServiceRatingCommand(
                body.Rating,
                body.CommentAr,
                body.CommentEn,
                body.Page,
                body.Locale);
            var id = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return Results.Created($"/api/surveys/service-rating/{id}", new { id });
        })
        .AllowAnonymous()
        .WithName("SubmitServiceRating");

        return app;
    }
}

public sealed record ServiceRatingRequest(
    int Rating,
    string? CommentAr,
    string? CommentEn,
    string Page,
    string Locale);
