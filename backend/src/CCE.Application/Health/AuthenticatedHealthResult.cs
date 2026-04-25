namespace CCE.Application.Health;

public sealed record AuthenticatedHealthResult(
    string Status,
    AuthenticatedUserInfo User,
    string Locale,
    DateTimeOffset UtcNow);

public sealed record AuthenticatedUserInfo(
    string Id,
    string PreferredUsername,
    string Email,
    string Upn,
    IReadOnlyList<string> Groups);
