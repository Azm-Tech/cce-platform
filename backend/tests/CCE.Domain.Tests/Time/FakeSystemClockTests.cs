using CCE.Domain.Common;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Time;

public class FakeSystemClockTests
{
    [Fact]
    public void Default_constructor_starts_at_default_reference_moment()
    {
        ISystemClock clock = new FakeSystemClock();

        clock.UtcNow.Should().Be(FakeSystemClock.DefaultStart);
    }

    [Fact]
    public void Constructor_with_explicit_start_uses_that_moment()
    {
        var moment = new DateTimeOffset(2030, 6, 15, 12, 0, 0, TimeSpan.Zero);
        ISystemClock clock = new FakeSystemClock(moment);

        clock.UtcNow.Should().Be(moment);
    }

    [Fact]
    public void Advance_moves_the_clock_forward()
    {
        var clock = new FakeSystemClock();
        var before = clock.UtcNow;

        clock.Advance(TimeSpan.FromHours(3));

        clock.UtcNow.Should().Be(before.AddHours(3));
    }

    [Fact]
    public void Advance_with_negative_duration_throws()
    {
        var clock = new FakeSystemClock();

        var act = () => clock.Advance(TimeSpan.FromSeconds(-1));

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("delta");
    }

    [Fact]
    public void SetTo_jumps_to_the_specified_moment()
    {
        var clock = new FakeSystemClock();
        var target = new DateTimeOffset(2027, 3, 14, 9, 26, 53, TimeSpan.Zero);

        clock.SetTo(target);

        clock.UtcNow.Should().Be(target);
    }
}
