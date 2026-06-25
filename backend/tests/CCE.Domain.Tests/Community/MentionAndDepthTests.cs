using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.TestInfrastructure.Time;

namespace CCE.Domain.Tests.Community;

public class MentionAndDepthTests
{
    [Fact]
    public void Mention_requires_ids()
    {
        var clock = new FakeSystemClock();
        var act = () => Mention.Create(MentionSourceType.Reply, System.Guid.Empty,
            System.Guid.NewGuid(), System.Guid.NewGuid(), "snippet",
            System.Guid.NewGuid(), System.Guid.NewGuid(), clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reply_nesting_beyond_max_depth_throws()
    {
        var clock = new FakeSystemClock();
        var postId = System.Guid.NewGuid();
        var current = PostReply.CreateRoot(postId, System.Guid.NewGuid(), "root", "en", false, clock);
        // Build a chain up to MaxDepth.
        for (var depth = 1; depth <= PostReply.MaxDepth; depth++)
            current = PostReply.CreateChild(current, System.Guid.NewGuid(), "c", "en", false, clock);

        var act = () => PostReply.CreateChild(current, System.Guid.NewGuid(), "too deep", "en", false, clock);
        act.Should().Throw<DomainException>().WithMessage("*depth*");
    }
}
