using CCE.Application.Assistant;

namespace CCE.Infrastructure.Assistant;

/// <summary>
/// Stub implementation of <see cref="ISmartAssistantClient"/>.
/// Returns a clearly-labelled placeholder reply.
/// Real LLM integration is deferred to Sub-project 8.
/// </summary>
public sealed class SmartAssistantClient : ISmartAssistantClient
{
    public Task<SmartAssistantReplyDto> AskAsync(string question, string locale, CancellationToken ct)
    {
        var reply = $"[STUB] Smart assistant integration coming in sub-project 8. Your question was: {question}";
        return Task.FromResult(new SmartAssistantReplyDto(reply));
    }
}
