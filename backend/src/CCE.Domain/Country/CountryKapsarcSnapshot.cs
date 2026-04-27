using CCE.Domain.Common;

namespace CCE.Domain.Country;

/// <summary>
/// One reading of the KAPSARC Circular Carbon Economy index for a country at a point in time.
/// Append-only by convention — once captured, fields are immutable. The pointer
/// <c>Country.LatestKapsarcSnapshotId</c> caches the most recent snapshot per country.
/// NOT audited (high-volume time-series; spec §4.11).
/// </summary>
public sealed class CountryKapsarcSnapshot : Entity<System.Guid>
{
    private CountryKapsarcSnapshot(
        System.Guid id,
        System.Guid countryId,
        string classification,
        decimal performanceScore,
        decimal totalIndex,
        System.DateTimeOffset snapshotTakenOn,
        string? sourceVersion) : base(id)
    {
        CountryId = countryId;
        Classification = classification;
        PerformanceScore = performanceScore;
        TotalIndex = totalIndex;
        SnapshotTakenOn = snapshotTakenOn;
        SourceVersion = sourceVersion;
    }

    public System.Guid CountryId { get; private set; }
    public string Classification { get; private set; }
    public decimal PerformanceScore { get; private set; }
    public decimal TotalIndex { get; private set; }
    public System.DateTimeOffset SnapshotTakenOn { get; private set; }
    public string? SourceVersion { get; private set; }

    public static CountryKapsarcSnapshot Capture(
        System.Guid countryId,
        string classification,
        decimal performanceScore,
        decimal totalIndex,
        ISystemClock clock,
        string? sourceVersion = null)
    {
        if (countryId == System.Guid.Empty) throw new DomainException("CountryId is required.");
        if (string.IsNullOrWhiteSpace(classification)) throw new DomainException("Classification is required.");
        if (performanceScore < 0 || performanceScore > 100)
        {
            throw new DomainException("PerformanceScore must be between 0 and 100.");
        }
        if (totalIndex < 0 || totalIndex > 100)
        {
            throw new DomainException("TotalIndex must be between 0 and 100.");
        }
        return new CountryKapsarcSnapshot(
            System.Guid.NewGuid(), countryId, classification,
            performanceScore, totalIndex, clock.UtcNow, sourceVersion);
    }
}
