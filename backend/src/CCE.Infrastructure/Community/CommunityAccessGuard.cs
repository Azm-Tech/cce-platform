using CCE.Application.Common.Interfaces;
using CCE.Application.Community;
using CCE.Domain.Community;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Community;

/// <summary>EF implementation of <see cref="ICommunityAccessGuard"/> (read-only).</summary>
public sealed class CommunityAccessGuard : ICommunityAccessGuard
{
    private readonly ICceDbContext _db;

    public CommunityAccessGuard(ICceDbContext db) => _db = db;

    public async Task<bool> CanReadAsync(Guid communityId, Guid? userId, CancellationToken ct)
    {
        var community = await _db.Communities
            .Where(c => c.Id == communityId && c.IsActive)
            .Select(c => new { c.Visibility })
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (community is null) return false;
        if (community.Visibility == CommunityVisibility.Public) return true;
        return userId is { } uid && await IsMemberAsync(communityId, uid, ct).ConfigureAwait(false);
    }

    public async Task<bool> CanPostAsync(Guid communityId, Guid userId, CancellationToken ct)
    {
        var active = await _db.Communities.AnyAsync(c => c.Id == communityId && c.IsActive, ct).ConfigureAwait(false);
        return active && await IsMemberAsync(communityId, userId, ct).ConfigureAwait(false);
    }

    public Task<bool> CanModerateAsync(Guid communityId, Guid userId, CancellationToken ct)
        => _db.CommunityMemberships.AnyAsync(
            m => m.CommunityId == communityId && m.UserId == userId && m.Role == CommunityRole.Moderator, ct);

    private Task<bool> IsMemberAsync(Guid communityId, Guid userId, CancellationToken ct)
        => _db.CommunityMemberships.AnyAsync(m => m.CommunityId == communityId && m.UserId == userId, ct);
}
