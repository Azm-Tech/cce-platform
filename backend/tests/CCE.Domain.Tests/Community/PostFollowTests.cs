using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Community;

public class PostFollowTests
{
    [Fact]
    public void Follow_creates_association()
    {
        var clock = new FakeSystemClock();
        var f = PostFollow.Follow(System.Guid.NewGuid(), System.Guid.NewGuid(), clock);
        f.FollowedOn.Should().Be(clock.UtcNow);
    }

    [Fact]
    public void Empty_postId_throws()
    {
        var clock = new FakeSystemClock();
        var act = () => PostFollow.Follow(System.Guid.Empty, System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Empty_userId_throws()
    {
        var clock = new FakeSystemClock();
        var act = () => PostFollow.Follow(System.Guid.NewGuid(), System.Guid.Empty, clock);
        act.Should().Throw<DomainException>();
    }
}
