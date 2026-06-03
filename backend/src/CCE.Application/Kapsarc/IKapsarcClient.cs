namespace CCE.Application.Kapsarc;

/// <summary>
/// Application-facing abstraction over the KAPSARC classification-verification service.
/// Mirrors the <c>IEmailSender</c> pattern: the Application layer depends on this interface,
/// Infrastructure provides the Refit-backed implementation.
/// </summary>
public interface IKapsarcClient
{
    Task<KapsarcClassificationResult> GetClassificationAsync(
        string countryCode,
        string countryName,
        CancellationToken ct = default);
}

/// <summary>
/// Domain-friendly result of a KAPSARC lookup. <see cref="Success"/> is false when the
/// service is unavailable or has no data for the country (→ BRD ER001).
/// </summary>
public sealed record KapsarcClassificationResult(
    bool Success,
    string? Classification,
    decimal? PerformanceScore,
    decimal? TotalIndex,
    string? Error)
{
    public static KapsarcClassificationResult Ok(string classification, decimal performanceScore, decimal totalIndex)
        => new(true, classification, performanceScore, totalIndex, null);

    public static KapsarcClassificationResult Unavailable(string? error)
        => new(false, null, null, null, error);
}
