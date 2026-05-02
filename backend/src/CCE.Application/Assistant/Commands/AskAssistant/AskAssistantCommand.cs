namespace CCE.Application.Assistant.Commands.AskAssistant;

/// <summary>
/// Streaming command. The handler does NOT use MediatR's IRequest&lt;TResponse&gt;
/// pattern (which is single-response Task-shaped). Instead the endpoint
/// constructs the stream directly via ISmartAssistantClient and writes it
/// through SseWriter — this keeps MediatR out of the streaming hot path.
/// We keep the command + validator as a typed boundary for inputs.
/// </summary>
public sealed record AskAssistantCommand(
    IReadOnlyList<ChatMessage> Messages,
    string Locale);
