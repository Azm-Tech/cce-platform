using CCE.Domain.Common;
using CCE.Domain.InteractiveCity;

namespace CCE.Domain.Tests.InteractiveCity;

public class CityTechnologyTests
{
    private static CityTechnology NewTech() => CityTechnology.Create(
        "ألواح", "Solar Panels", "وصف", "Description", "الطاقة", "Energy",
        carbonImpactKgPerYear: -2500m, costUsd: 15000m);

    [Fact]
    public void Create_active_with_negative_carbon_impact_for_reductions() {
        var t = NewTech();
        t.IsActive.Should().BeTrue();
        t.CarbonImpactKgPerYear.Should().Be(-2500m);
    }

    [Fact]
    public void Negative_cost_throws() {
        var act = () => CityTechnology.Create(
            "ا", "x", "ا", "x", "ا", "x", -100m, -1m);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Empty_required_field_throws() {
        var act = () => CityTechnology.Create(
            "", "x", "ا", "x", "ا", "x", 0m, 0m);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateImpact_changes_values() {
        var t = NewTech();
        t.UpdateImpact(-3000m, 18000m);
        t.CarbonImpactKgPerYear.Should().Be(-3000m);
        t.CostUsd.Should().Be(18000m);
    }

    [Fact]
    public void Deactivate_then_Activate_toggles() {
        var t = NewTech();
        t.Deactivate();
        t.IsActive.Should().BeFalse();
        t.Activate();
        t.IsActive.Should().BeTrue();
    }
}
