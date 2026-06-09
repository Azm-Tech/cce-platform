using CCE.Domain.Common;

namespace CCE.Domain.Community;

/// <summary>
/// One user's up/down vote on a reply (<c>+1</c> or <c>-1</c>). Unique on <c>(ReplyId, UserId)</c>.
/// NOT audited (high-volume). <see cref="PostReply"/> carries the denormalized counters.
/// </summary>
public sealed class ReplyVote : Entity<System.Guid>
{
    private ReplyVote(System.Guid id, System.Guid replyId, System.Guid userId,
        int value, System.DateTimeOffset votedOn) : base(id)
    {
        ReplyId = replyId;
        UserId = userId;
        Value = value;
        VotedOn = votedOn;
    }

    public System.Guid ReplyId { get; private set; }
    public System.Guid UserId { get; private set; }

    /// <summary>+1 for an upvote, -1 for a downvote.</summary>
    public int Value { get; private set; }
    public System.DateTimeOffset VotedOn { get; private set; }

    public static ReplyVote Cast(System.Guid replyId, System.Guid userId, int value, ISystemClock clock)
    {
        if (replyId == System.Guid.Empty) throw new DomainException("ReplyId is required.");
        if (userId == System.Guid.Empty) throw new DomainException("UserId is required.");
        if (value is not (1 or -1)) throw new DomainException("Vote value must be +1 or -1.");
        return new ReplyVote(System.Guid.NewGuid(), replyId, userId, value, clock.UtcNow);
    }

    public void ChangeTo(int value, ISystemClock clock)
    {
        if (value is not (1 or -1)) throw new DomainException("Vote value must be +1 or -1.");
        Value = value;
        VotedOn = clock.UtcNow;
    }
}
