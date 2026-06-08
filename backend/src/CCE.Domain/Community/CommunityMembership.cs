using CCE.Domain.Common;

namespace CCE.Domain.Community;

/// <summary>Membership of a user in a community. Unique on <c>(CommunityId, UserId)</c>. NOT audited.</summary>
public sealed class CommunityMembership : Entity<System.Guid>
{
    private CommunityMembership(System.Guid id, System.Guid communityId, System.Guid userId,
        CommunityRole role, System.DateTimeOffset joinedOn) : base(id)
    {
        CommunityId = communityId;
        UserId = userId;
        Role = role;
        JoinedOn = joinedOn;
    }

    public System.Guid CommunityId { get; private set; }
    public System.Guid UserId { get; private set; }
    public CommunityRole Role { get; private set; }
    public System.DateTimeOffset JoinedOn { get; private set; }

    public static CommunityMembership Join(System.Guid communityId, System.Guid userId,
        CommunityRole role, ISystemClock clock)
    {
        if (communityId == System.Guid.Empty) throw new DomainException("CommunityId is required.");
        if (userId == System.Guid.Empty) throw new DomainException("UserId is required.");
        return new CommunityMembership(System.Guid.NewGuid(), communityId, userId, role, clock.UtcNow);
    }

    public void Promote() => Role = CommunityRole.Moderator;
}
