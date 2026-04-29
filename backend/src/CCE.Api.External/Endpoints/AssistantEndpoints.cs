using CCE.Application.Assistant.Commands.AskAssistant;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class AssistantEndpoints
{
    public static IEndpointRouteBuilder MapAssistantEndpoints(this IEndpointRouteBuilder app)
    {
        var assistant = app.MapGroup("/api/assistant").WithTags("Assistant");

        // POST /api/assistant/query
        assistant.MapPost("/query", async (
            AskAssistantRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var cmd = new AskAssistantCommand(body.Question, body.Locale);
            var reply = await mediator.Send(cmd, ct).ConfigureAwait(false);
            return Results.Ok(reply);
        })
        .AllowAnonymous()
        .WithName("AskAssistant");

        return app;
    }
}

public sealed record AskAssistantRequest(string Question, string Locale);
