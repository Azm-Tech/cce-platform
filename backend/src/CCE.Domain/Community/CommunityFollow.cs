using CCE.Domain.Common;

namespace CCE.Domain.Community;

/// <summary>
/// A user following a community for feed/notification purposes. Distinct from membership
/// (anyone can follow a public community; membership gates posting and private reads).
/// Unique on <c>(CommunityId, UserId)</c>. NOT audited.
/// </summary>
public sealed class CommunityFollow : Entity<System.Guid>
{
    private CommunityFollow(System.Guid id, System.Guid communityId, System.Guid userId,
        System.DateTimeOffset followedOn) : base(id)
    {
        CommunityId = communityId;
        UserId = userId;
        FollowedOn = followedOn;
    }

    public System.Guid CommunityId { get; private set; }
    public System.Guid UserId { get; private set; }
    public System.DateTimeOffset FollowedOn { get; private set; }

    public static CommunityFollow Follow(System.Guid communityId, System.Guid userId, ISystemClock clock)
    {
        if (communityId == System.Guid.Empty) throw new DomainException("CommunityId is required.");
        if (userId == System.Guid.Empty) throw new DomainException("UserId is required.");
        return new CommunityFollow(System.Guid.NewGuid(), communityId, userId, clock.UtcNow);
    }
}
