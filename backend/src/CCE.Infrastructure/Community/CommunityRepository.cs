using CCE.Application.Community;
using CCE.Domain.Community;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Community;

/// <summary>EF implementation of <see cref="ICommunityRepository"/>; returns tracked entities for the UoW.</summary>
public sealed class CommunityRepository : ICommunityRepository
{
    private readonly CceDbContext _db;

    public CommunityRepository(CceDbContext db) => _db = db;

    public Task<Domain.Community.Community?> GetAsync(Guid id, CancellationToken ct)
        => _db.Communities.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct)
        => _db.Communities.AnyAsync(c => c.Slug == slug, ct);

    public Task<CommunityMembership?> FindMembershipAsync(Guid communityId, Guid userId, CancellationToken ct)
        => _db.CommunityMemberships.FirstOrDefaultAsync(m => m.CommunityId == communityId && m.UserId == userId, ct);

    public Task<bool> HasMembershipAsync(Guid communityId, Guid userId, CancellationToken ct)
        => _db.CommunityMemberships.AnyAsync(m => m.CommunityId == communityId && m.UserId == userId, ct);

    public Task<CommunityFollow?> FindFollowAsync(Guid communityId, Guid userId, CancellationToken ct)
        => _db.CommunityFollows.FirstOrDefaultAsync(f => f.CommunityId == communityId && f.UserId == userId, ct);

    public Task<bool> HasPendingRequestAsync(Guid communityId, Guid userId, CancellationToken ct)
        => _db.CommunityJoinRequests.AnyAsync(
            r => r.CommunityId == communityId && r.UserId == userId && r.Status == JoinRequestStatus.Pending, ct);

    public Task<CommunityJoinRequest?> GetRequestAsync(Guid requestId, CancellationToken ct)
        => _db.CommunityJoinRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct);

    public void AddCommunity(Domain.Community.Community community) => _db.Communities.Add(community);
    public void AddMembership(CommunityMembership membership) => _db.CommunityMemberships.Add(membership);
    public void RemoveMembership(CommunityMembership membership) => _db.CommunityMemberships.Remove(membership);
    public void AddFollow(CommunityFollow follow) => _db.CommunityFollows.Add(follow);
    public void RemoveFollow(CommunityFollow follow) => _db.CommunityFollows.Remove(follow);
    public void AddJoinRequest(CommunityJoinRequest request) => _db.CommunityJoinRequests.Add(request);
}
