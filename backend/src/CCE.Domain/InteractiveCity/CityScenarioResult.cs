using CCE.Domain.Common;

namespace CCE.Domain.InteractiveCity;

/// <summary>
/// Append-only computation result for a <see cref="CityScenario"/>. The simulation engine
/// produces one of these per run; the front-end charts the latest one. NOT audited (high-volume).
/// </summary>
public sealed class CityScenarioResult : Entity<System.Guid>
{
    private CityScenarioResult(System.Guid id, System.Guid scenarioId,
        int? computedCarbonNeutralityYear, decimal computedTotalCostUsd,
        System.DateTimeOffset computedAt, string engineVersion) : base(id)
    {
        ScenarioId = scenarioId;
        ComputedCarbonNeutralityYear = computedCarbonNeutralityYear;
        ComputedTotalCostUsd = computedTotalCostUsd;
        ComputedAt = computedAt;
        EngineVersion = engineVersion;
    }

    public System.Guid ScenarioId { get; private set; }
    public int? ComputedCarbonNeutralityYear { get; private set; }
    public decimal ComputedTotalCostUsd { get; private set; }
    public System.DateTimeOffset ComputedAt { get; private set; }
    public string EngineVersion { get; private set; }

    public static CityScenarioResult Compute(System.Guid scenarioId,
        int? computedCarbonNeutralityYear, decimal computedTotalCostUsd,
        string engineVersion, ISystemClock clock)
    {
        if (scenarioId == System.Guid.Empty) throw new DomainException("ScenarioId is required.");
        if (string.IsNullOrWhiteSpace(engineVersion))
            throw new DomainException("EngineVersion is required.");
        if (computedTotalCostUsd < 0)
            throw new DomainException("ComputedTotalCostUsd cannot be negative.");
        return new CityScenarioResult(System.Guid.NewGuid(), scenarioId,
            computedCarbonNeutralityYear, computedTotalCostUsd, clock.UtcNow, engineVersion);
    }
}
