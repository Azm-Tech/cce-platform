namespace CCE.Application.Community.Moderation;

/// <summary>
/// Well-known phase identifiers stored on <c>ModerationRecord.Phase</c>
/// and used in <see cref="IAiModerationProvider"/> results.
/// </summary>
public static class ModerationPhase
{
    public const string Rule  = "rule";
    public const string Ai    = "ai";
    public const string Human = "human";
}
