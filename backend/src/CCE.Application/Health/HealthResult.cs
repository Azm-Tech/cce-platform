namespace CCE.Application.Health;

public sealed record HealthResult(
    string Status,
    string Version,
    string Locale,
    DateTimeOffset UtcNow);
