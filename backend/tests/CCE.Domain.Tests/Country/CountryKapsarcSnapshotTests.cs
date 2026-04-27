using CCE.Domain.Common;
using CCE.Domain.Country;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Country;

public class CountryKapsarcSnapshotTests
{
    [Fact]
    public void Capture_creates_snapshot()
    {
        var clock = new FakeSystemClock();
        var s = CountryKapsarcSnapshot.Capture(
            System.Guid.NewGuid(), "Leader", 87.5m, 92.0m, clock, "v2.4");

        s.Classification.Should().Be("Leader");
        s.PerformanceScore.Should().Be(87.5m);
        s.TotalIndex.Should().Be(92.0m);
        s.SnapshotTakenOn.Should().Be(clock.UtcNow);
        s.SourceVersion.Should().Be("v2.4");
    }

    [Fact]
    public void Capture_with_negative_score_throws()
    {
        var clock = new FakeSystemClock();
        var act = () => CountryKapsarcSnapshot.Capture(
            System.Guid.NewGuid(), "x", -1m, 50m, clock);
        act.Should().Throw<DomainException>().WithMessage("*PerformanceScore*");
    }

    [Fact]
    public void Capture_with_score_above_100_throws()
    {
        var clock = new FakeSystemClock();
        var act = () => CountryKapsarcSnapshot.Capture(
            System.Guid.NewGuid(), "x", 100.1m, 50m, clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Capture_with_empty_classification_throws()
    {
        var clock = new FakeSystemClock();
        var act = () => CountryKapsarcSnapshot.Capture(
            System.Guid.NewGuid(), "", 50m, 50m, clock);
        act.Should().Throw<DomainException>().WithMessage("*Classification*");
    }

    [Fact]
    public void Snapshot_is_NOT_audited()
    {
        var attrs = typeof(CountryKapsarcSnapshot).GetCustomAttributes(typeof(AuditedAttribute), inherit: false);
        attrs.Should().BeEmpty(because: "high-volume time-series — spec §4.11 excludes from audit");
    }
}
