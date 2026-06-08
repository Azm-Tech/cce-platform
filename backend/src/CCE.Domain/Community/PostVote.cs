using CCE.Domain.Common;

namespace CCE.Domain.Community;

/// <summary>
/// One user's up/down vote on a post (<c>+1</c> or <c>-1</c>). Uniqueness is enforced by a
/// unique index on <c>(PostId, UserId)</c>. NOT audited (high-volume association — spec §4.11);
/// the per-user row is the source of truth, while <see cref="Post"/> carries denormalized counters.
/// </summary>
public sealed class PostVote : Entity<System.Guid>
{
    private PostVote(System.Guid id, System.Guid postId, System.Guid userId,
        int value, System.DateTimeOffset votedOn) : base(id)
    {
        PostId = postId;
        UserId = userId;
        Value = value;
        VotedOn = votedOn;
    }

    public System.Guid PostId { get; private set; }
    public System.Guid UserId { get; private set; }

    /// <summary>+1 for an upvote, -1 for a downvote.</summary>
    public int Value { get; private set; }
    public System.DateTimeOffset VotedOn { get; private set; }

    public static PostVote Cast(System.Guid postId, System.Guid userId, int value, ISystemClock clock)
    {
        if (postId == System.Guid.Empty) throw new DomainException("PostId is required.");
        if (userId == System.Guid.Empty) throw new DomainException("UserId is required.");
        if (value is not (1 or -1)) throw new DomainException("Vote value must be +1 or -1.");
        return new PostVote(System.Guid.NewGuid(), postId, userId, value, clock.UtcNow);
    }

    public void ChangeTo(int value, ISystemClock clock)
    {
        if (value is not (1 or -1)) throw new DomainException("Vote value must be +1 or -1.");
        Value = value;
        VotedOn = clock.UtcNow;
    }
}
