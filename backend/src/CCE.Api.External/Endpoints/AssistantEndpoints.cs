using CCE.Application.Assistant;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CCE.Api.External.Endpoints;

public static class AssistantEndpoints
{
    public static IEndpointRouteBuilder MapAssistantEndpoints(this IEndpointRouteBuilder app)
    {
        var assistant = app.MapGroup("/api/assistant").WithTags("Assistant");

        // POST /api/assistant/query — streams text/event-stream
        assistant.MapPost("/query", async (
            AskAssistantRequest body,
            ISmartAssistantClient client,
            HttpResponse response,
            CancellationToken ct) =>
        {
            var messages = (body.Messages ?? Array.Empty<ChatMessageDto>())
                .Select(m => new ChatMessage(m.Role ?? "", m.Content ?? ""))
                .ToList();
            var stream = client.StreamAsync(messages, body.Locale ?? "en", ct);
            await SseWriter.WriteAsync(response, stream, ct).ConfigureAwait(false);
        })
        .AllowAnonymous()
        .WithName("AskAssistant");

        return app;
    }
}

public sealed record AskAssistantRequest(IReadOnlyList<ChatMessageDto> Messages, string Locale);
public sealed record ChatMessageDto(string Role, string Content);
