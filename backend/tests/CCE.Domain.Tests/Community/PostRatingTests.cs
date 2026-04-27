using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Community;

public class PostRatingTests
{
    [Fact]
    public void Rate_with_valid_stars()
    {
        var clock = new FakeSystemClock();
        var r = PostRating.Rate(System.Guid.NewGuid(), System.Guid.NewGuid(), 4, clock);
        r.Stars.Should().Be(4);
        r.RatedOn.Should().Be(clock.UtcNow);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Rate_with_out_of_range_throws(int stars)
    {
        var clock = new FakeSystemClock();
        var act = () => PostRating.Rate(System.Guid.NewGuid(), System.Guid.NewGuid(), stars, clock);
        act.Should().Throw<DomainException>().WithMessage("*Stars*");
    }

    [Fact]
    public void Update_replaces_stars_and_ratedOn()
    {
        var clock = new FakeSystemClock();
        var r = PostRating.Rate(System.Guid.NewGuid(), System.Guid.NewGuid(), 3, clock);
        clock.Advance(System.TimeSpan.FromHours(1));

        r.Update(5, clock);

        r.Stars.Should().Be(5);
        r.RatedOn.Should().Be(clock.UtcNow);
    }

    [Fact]
    public void PostRating_is_NOT_audited()
    {
        var attrs = typeof(PostRating).GetCustomAttributes(typeof(AuditedAttribute), inherit: false);
        attrs.Should().BeEmpty(because: "high-volume association per spec §4.11");
    }
}
