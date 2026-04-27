using CCE.Domain.Common;
using CCE.Domain.InteractiveCity;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.InteractiveCity;

public class CityScenarioResultTests
{
    [Fact]
    public void Compute_creates_result() {
        var clock = new FakeSystemClock();
        var r = CityScenarioResult.Compute(
            System.Guid.NewGuid(), 2055, 120_000m, "engine-v1.2", clock);
        r.ComputedCarbonNeutralityYear.Should().Be(2055);
        r.EngineVersion.Should().Be("engine-v1.2");
    }

    [Fact]
    public void Compute_with_null_neutrality_year_allowed() {
        var clock = new FakeSystemClock();
        var r = CityScenarioResult.Compute(
            System.Guid.NewGuid(), null, 50_000m, "v1", clock);
        r.ComputedCarbonNeutralityYear.Should().BeNull();
    }

    [Fact]
    public void Negative_cost_throws() {
        var clock = new FakeSystemClock();
        var act = () => CityScenarioResult.Compute(
            System.Guid.NewGuid(), 2050, -1m, "v1", clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Empty_engineVersion_throws() {
        var clock = new FakeSystemClock();
        var act = () => CityScenarioResult.Compute(
            System.Guid.NewGuid(), 2050, 100m, "", clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Result_is_NOT_audited() {
        typeof(CityScenarioResult).GetCustomAttributes(typeof(AuditedAttribute), inherit: false)
            .Should().BeEmpty();
    }
}
