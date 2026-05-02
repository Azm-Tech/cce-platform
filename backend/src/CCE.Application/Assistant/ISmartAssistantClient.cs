namespace CCE.Application.Assistant;

/// <summary>
/// Abstraction over the smart-assistant LLM backend. Streams typed
/// SseEvent records (text chunks, citations, done, error) as they're
/// produced. Production LLM provider is a future swap-in; the stub
/// in CCE.Infrastructure fake-streams placeholder text.
/// </summary>
public interface ISmartAssistantClient
{
    IAsyncEnumerable<SseEvent> StreamAsync(
        IReadOnlyList<ChatMessage> messages,
        string locale,
        CancellationToken ct);
}
