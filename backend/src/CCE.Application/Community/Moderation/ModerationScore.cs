namespace CCE.Application.Community.Moderation;

public sealed record ModerationScore(
    bool    IsSafe,
    float   Confidence,
    string  Category,
    string? Reason);
