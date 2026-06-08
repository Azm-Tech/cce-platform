using CCE.Domain.Community;

namespace CCE.Application.Community;

/// <summary>Write-side repository for the community aggregate and its associations (§A.1).</summary>
public interface ICommunityRepository
{
    Task<Domain.Community.Community?> GetAsync(Guid id, CancellationToken ct);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct);
    Task<CommunityMembership?> FindMembershipAsync(Guid communityId, Guid userId, CancellationToken ct);
    Task<bool> HasMembershipAsync(Guid communityId, Guid userId, CancellationToken ct);
    Task<CommunityFollow?> FindFollowAsync(Guid communityId, Guid userId, CancellationToken ct);
    Task<bool> HasPendingRequestAsync(Guid communityId, Guid userId, CancellationToken ct);
    Task<CommunityJoinRequest?> GetRequestAsync(Guid requestId, CancellationToken ct);

    void AddCommunity(Domain.Community.Community community);
    void AddMembership(CommunityMembership membership);
    void RemoveMembership(CommunityMembership membership);
    void AddFollow(CommunityFollow follow);
    void RemoveFollow(CommunityFollow follow);
    void AddJoinRequest(CommunityJoinRequest request);
}
