using CCE.Domain.Common;

namespace CCE.Domain.Content;

public sealed class NewsFollow : AuditableEntity<System.Guid>
{
    private NewsFollow(System.Guid id, System.Guid userId) : base(id)
    {
        UserId = userId;
        Status = FollowStatus.Followed;
    }

    public System.Guid UserId { get; private set; }
    public FollowStatus Status { get; private set; }
    public System.DateTimeOffset? UnfollowedOn { get; private set; }

    public static NewsFollow Follow(System.Guid userId, System.Guid createdBy, ISystemClock clock)
    {
        if (userId == System.Guid.Empty) throw new DomainException("UserId is required.");
        var follow = new NewsFollow(System.Guid.NewGuid(), userId);
        follow.MarkAsCreated(createdBy, clock);
        return follow;
    }

    public void ReFollow(System.Guid modifiedBy, ISystemClock clock)
    {
        Status = FollowStatus.Followed;
        UnfollowedOn = null;
        MarkAsModified(modifiedBy, clock);
    }

    public void Unfollow(System.Guid modifiedBy, ISystemClock clock)
    {
        Status = FollowStatus.Unfollowed;
        UnfollowedOn = clock.UtcNow;
        MarkAsModified(modifiedBy, clock);
    }
}
