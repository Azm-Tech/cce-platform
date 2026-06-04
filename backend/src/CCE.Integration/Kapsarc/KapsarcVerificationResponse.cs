namespace CCE.Integration.Kapsarc;

/// <summary>
/// Raw KAPSARC gateway response. <see cref="Status"/> is "success" on a hit;
/// otherwise <see cref="Error"/> carries the reason and the metric fields are null.
/// </summary>
public sealed record KapsarcVerificationResponse(
    string Status,
    string? Classification = null,
    decimal? PerformanceScore = null,
    decimal? TotalIndex = null,
    string? Error = null);
