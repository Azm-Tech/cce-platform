namespace CCE.Application.Assistant;

/// <summary>
/// Abstraction over the smart-assistant LLM backend.
/// The production implementation (LLM provider) is deferred to Sub-project 8.
/// The stub implementation in CCE.Infrastructure returns a placeholder reply.
/// </summary>
public interface ISmartAssistantClient
{
    /// <summary>
    /// Sends <paramref name="question"/> to the assistant and returns a reply.
    /// </summary>
    Task<SmartAssistantReplyDto> AskAsync(string question, string locale, CancellationToken ct);
}
