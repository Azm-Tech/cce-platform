namespace CCE.Application.Community.Moderation;

/// <summary>
/// Well-known AI category labels returned by <see cref="IAiModerationProvider.ModerateAsync"/>
/// and written to <c>ModerationRecord.Category</c>.
/// </summary>
public static class ModerationCategory
{
    public const string Safe        = "safe";
    public const string Spam        = "spam";
    public const string Hate        = "hate";
    public const string Explicit    = "explicit";
    public const string Harassment  = "harassment";
    public const string ParseError  = "parse-error";
    public const string RateLimited = "rate-limited";
}
