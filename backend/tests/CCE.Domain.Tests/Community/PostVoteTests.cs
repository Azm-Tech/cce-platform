using CCE.Domain.Common;
using CCE.Domain.Community;

namespace CCE.Domain.Tests.Community;

public class PostVoteTests
{
    private static ISystemClock Clock()
    {
        var clock = Substitute.For<ISystemClock>();
        clock.UtcNow.Returns(System.DateTimeOffset.UtcNow);
        return clock;
    }

    private static Post NewPost(ISystemClock clock)
        => Post.CreateDraft(System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid(),
            PostType.Info, "Title", "Body", "en", clock);

    [Fact]
    public void Cast_rejects_values_other_than_plus_or_minus_one()
    {
        var clock = Clock();
        var act = () => PostVote.Cast(System.Guid.NewGuid(), System.Guid.NewGuid(), 2, clock);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ApplyVote_up_then_flip_to_down_then_retract()
    {
        var post = NewPost(Clock());

        post.ApplyVote(0, 1);
        post.UpvoteCount.Should().Be(1);
        post.DownvoteCount.Should().Be(0);

        post.ApplyVote(1, -1);
        post.UpvoteCount.Should().Be(0);
        post.DownvoteCount.Should().Be(1);

        post.ApplyVote(-1, 0);
        post.UpvoteCount.Should().Be(0);
        post.DownvoteCount.Should().Be(0);
    }

    [Fact]
    public void ApplyVote_is_idempotent_when_value_unchanged()
    {
        var post = NewPost(Clock());
        post.ApplyVote(0, 1);
        var score = post.Score;

        post.ApplyVote(1, 1);

        post.UpvoteCount.Should().Be(1);
        post.Score.Should().Be(score);
    }

    [Fact]
    public void Many_upvotes_raise_the_score_above_baseline()
    {
        // The hot rank uses log10(max(|net|,1)), so a single vote is intentionally flat;
        // the order term only moves once the net climbs past 1.
        var post = NewPost(Clock());
        var baseline = post.Score;
        for (var i = 0; i < 20; i++) post.ApplyVote(0, 1);
        post.UpvoteCount.Should().Be(20);
        post.Score.Should().BeGreaterThan(baseline);
    }
}
