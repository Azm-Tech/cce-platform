using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Community;

public class UserFollowTests
{
    [Fact]
    public void Follow_creates_association()
    {
        var clock = new FakeSystemClock();
        var f = UserFollow.Follow(System.Guid.NewGuid(), System.Guid.NewGuid(), clock);
        f.FollowedOn.Should().Be(clock.UtcNow);
    }

    [Fact]
    public void Self_follow_throws()
    {
        var clock = new FakeSystemClock();
        var same = System.Guid.NewGuid();
        var act = () => UserFollow.Follow(same, same, clock);
        act.Should().Throw<DomainException>().WithMessage("*themselves*");
    }

    [Fact]
    public void Follow_with_empty_followerId_throws()
    {
        var clock = new FakeSystemClock();
        var act = () => UserFollow.Follow(System.Guid.Empty, System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Follow_with_empty_followedId_throws()
    {
        var clock = new FakeSystemClock();
        var act = () => UserFollow.Follow(System.Guid.NewGuid(), System.Guid.Empty, clock);
        act.Should().Throw<DomainException>();
    }
}
