namespace CCE.Application.Community.Moderation;

/// <summary>
/// Fast, synchronous, zero-API-call pre-filter. Catches obvious violations (short content,
/// URL-only posts, keyword denylist) before the AI provider is consulted.
/// </summary>
public interface IRuleBasedPreFilter
{
    bool ShouldFlag(string content, out string reason);
}
