namespace CCE.Application.Community.Moderation;

/// <summary>
/// Abstraction over AI-based content moderation. Implementations (Ollama, Groq/OpenRouter)
/// live in Infrastructure. Registered conditionally based on <c>Moderation:Provider</c> config.
/// </summary>
public interface IAiModerationProvider
{
    string ProviderName { get; }
    Task<ModerationScore> ModerateAsync(string content, CancellationToken ct);
}
