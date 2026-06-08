using CCE.Domain.Common;

namespace CCE.Domain.Community;

/// <summary>
/// A request to join a private community. A user may have at most one pending request per community
/// (partial-unique index). Approving it is the caller's cue to create the membership.
/// </summary>
public sealed class CommunityJoinRequest : Entity<System.Guid>
{
    private CommunityJoinRequest(System.Guid id, System.Guid communityId, System.Guid userId,
        System.DateTimeOffset requestedOn) : base(id)
    {
        CommunityId = communityId;
        UserId = userId;
        Status = JoinRequestStatus.Pending;
        RequestedOn = requestedOn;
    }

    public System.Guid CommunityId { get; private set; }
    public System.Guid UserId { get; private set; }
    public JoinRequestStatus Status { get; private set; }
    public System.DateTimeOffset RequestedOn { get; private set; }
    public System.Guid? DecidedById { get; private set; }
    public System.DateTimeOffset? DecidedOn { get; private set; }

    public static CommunityJoinRequest Submit(System.Guid communityId, System.Guid userId, ISystemClock clock)
    {
        if (communityId == System.Guid.Empty) throw new DomainException("CommunityId is required.");
        if (userId == System.Guid.Empty) throw new DomainException("UserId is required.");
        return new CommunityJoinRequest(System.Guid.NewGuid(), communityId, userId, clock.UtcNow);
    }

    public void Approve(System.Guid by, ISystemClock clock) => Decide(JoinRequestStatus.Approved, by, clock);

    public void Reject(System.Guid by, ISystemClock clock) => Decide(JoinRequestStatus.Rejected, by, clock);

    private void Decide(JoinRequestStatus status, System.Guid by, ISystemClock clock)
    {
        if (Status != JoinRequestStatus.Pending)
            throw new DomainException("Only pending join requests can be decided.");
        Status = status;
        DecidedById = by;
        DecidedOn = clock.UtcNow;
    }
}
