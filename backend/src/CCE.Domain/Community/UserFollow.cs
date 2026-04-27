using CCE.Domain.Common;

namespace CCE.Domain.Community;

/// <summary>User-follows-user. <c>FollowerId ≠ FollowedId</c> invariant.</summary>
public sealed class UserFollow : Entity<System.Guid>
{
    private UserFollow(System.Guid id, System.Guid followerId, System.Guid followedId,
        System.DateTimeOffset followedOn) : base(id)
    {
        FollowerId = followerId; FollowedId = followedId; FollowedOn = followedOn;
    }

    public System.Guid FollowerId { get; private set; }
    public System.Guid FollowedId { get; private set; }
    public System.DateTimeOffset FollowedOn { get; private set; }

    public static UserFollow Follow(System.Guid followerId, System.Guid followedId, ISystemClock clock)
    {
        if (followerId == System.Guid.Empty) throw new DomainException("FollowerId is required.");
        if (followedId == System.Guid.Empty) throw new DomainException("FollowedId is required.");
        if (followerId == followedId)
        {
            throw new DomainException("Users cannot follow themselves.");
        }
        return new UserFollow(System.Guid.NewGuid(), followerId, followedId, clock.UtcNow);
    }
}
