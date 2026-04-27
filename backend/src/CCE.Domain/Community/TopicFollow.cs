using CCE.Domain.Common;

namespace CCE.Domain.Community;

/// <summary>User-follows-topic association. Unique (TopicId, UserId) at Phase 08.
/// NOT audited per spec §4.11.</summary>
public sealed class TopicFollow : Entity<System.Guid>
{
    private TopicFollow(System.Guid id, System.Guid topicId, System.Guid userId,
        System.DateTimeOffset followedOn) : base(id)
    {
        TopicId = topicId; UserId = userId; FollowedOn = followedOn;
    }

    public System.Guid TopicId { get; private set; }
    public System.Guid UserId { get; private set; }
    public System.DateTimeOffset FollowedOn { get; private set; }

    public static TopicFollow Follow(System.Guid topicId, System.Guid userId, ISystemClock clock)
    {
        if (topicId == System.Guid.Empty) throw new DomainException("TopicId is required.");
        if (userId == System.Guid.Empty) throw new DomainException("UserId is required.");
        return new TopicFollow(System.Guid.NewGuid(), topicId, userId, clock.UtcNow);
    }
}
