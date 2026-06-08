using CCE.Domain.Common;

namespace CCE.Domain.Community;

/// <summary>A user's vote for a poll option. NOT audited.</summary>
public sealed class PollVote : Entity<System.Guid>
{
    private PollVote(System.Guid id, System.Guid pollId, System.Guid pollOptionId,
        System.Guid userId, System.DateTimeOffset votedOn) : base(id)
    {
        PollId = pollId;
        PollOptionId = pollOptionId;
        UserId = userId;
        VotedOn = votedOn;
    }

    public System.Guid PollId { get; private set; }
    public System.Guid PollOptionId { get; private set; }
    public System.Guid UserId { get; private set; }
    public System.DateTimeOffset VotedOn { get; private set; }

    public static PollVote Cast(System.Guid pollId, System.Guid pollOptionId, System.Guid userId, ISystemClock clock)
    {
        if (pollId == System.Guid.Empty) throw new DomainException("PollId is required.");
        if (pollOptionId == System.Guid.Empty) throw new DomainException("PollOptionId is required.");
        if (userId == System.Guid.Empty) throw new DomainException("UserId is required.");
        return new PollVote(System.Guid.NewGuid(), pollId, pollOptionId, userId, clock.UtcNow);
    }
}
