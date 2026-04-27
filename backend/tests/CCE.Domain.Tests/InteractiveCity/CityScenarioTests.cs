using CCE.Domain.Common;
using CCE.Domain.InteractiveCity;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.InteractiveCity;

public class CityScenarioTests
{
    private static CityScenario NewScenario(FakeSystemClock clock) => CityScenario.Create(
        System.Guid.NewGuid(), "خطتي", "My Plan", CityType.Mixed,
        2050, "{\"techs\": []}", clock);

    [Fact]
    public void Create_scenario() {
        var s = NewScenario(new FakeSystemClock());
        s.CityType.Should().Be(CityType.Mixed);
        s.TargetYear.Should().Be(2050);
        s.IsDeleted.Should().BeFalse();
    }

    [Theory]
    [InlineData(2025)]
    [InlineData(2090)]
    public void TargetYear_outside_range_throws(int badYear) {
        var clock = new FakeSystemClock();
        var act = () => CityScenario.Create(System.Guid.NewGuid(), "ا", "x",
            CityType.Coastal, badYear, "{}", clock);
        act.Should().Throw<DomainException>().WithMessage("*TargetYear*");
    }

    [Fact]
    public void UpdateConfiguration_advances_LastModifiedOn() {
        var clock = new FakeSystemClock();
        var s = NewScenario(clock);
        clock.Advance(System.TimeSpan.FromHours(2));
        s.UpdateConfiguration("{\"techs\": [\"solar\"]}", clock);
        s.LastModifiedOn.Should().Be(clock.UtcNow);
        s.ConfigurationJson.Should().Contain("solar");
    }

    [Fact]
    public void Rename_updates_names_and_modified() {
        var clock = new FakeSystemClock();
        var s = NewScenario(clock);
        clock.Advance(System.TimeSpan.FromHours(1));
        s.Rename("جديد", "New", clock);
        s.NameEn.Should().Be("New");
        s.LastModifiedOn.Should().Be(clock.UtcNow);
    }

    [Fact]
    public void SoftDelete_marks_deleted() {
        var clock = new FakeSystemClock();
        var s = NewScenario(clock);
        s.SoftDelete(System.Guid.NewGuid(), clock);
        s.IsDeleted.Should().BeTrue();
    }
}
