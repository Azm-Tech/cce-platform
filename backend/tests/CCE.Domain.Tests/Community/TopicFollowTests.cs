using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Community;

public class TopicFollowTests
{
    [Fact]
    public void Follow_creates_association()
    {
        var clock = new FakeSystemClock();
        var f = TopicFollow.Follow(System.Guid.NewGuid(), System.Guid.NewGuid(), clock);
        f.FollowedOn.Should().Be(clock.UtcNow);
    }

    [Fact]
    public void Follow_with_empty_topicId_throws()
    {
        var clock = new FakeSystemClock();
        var act = () => TopicFollow.Follow(System.Guid.Empty, System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Follow_with_empty_userId_throws()
    {
        var clock = new FakeSystemClock();
        var act = () => TopicFollow.Follow(System.Guid.NewGuid(), System.Guid.Empty, clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void TopicFollow_is_NOT_audited()
    {
        var attrs = typeof(TopicFollow).GetCustomAttributes(typeof(AuditedAttribute), inherit: false);
        attrs.Should().BeEmpty();
    }
}
