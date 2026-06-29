using CCE.Application.Community.Moderation;

namespace CCE.Infrastructure.Moderation;

/// <summary>Used when <c>Moderation:Provider = "None"</c>. Returns Approved immediately.</summary>
public sealed class NullAiModerationProvider : IAiModerationProvider
{
    public string ProviderName => "none";

    public Task<ModerationScore> ModerateAsync(string content, CancellationToken ct)
        => Task.FromResult(new ModerationScore(true, 1f, "safe", null));
}
