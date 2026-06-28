using System.Collections.Generic;
using System.Linq;
using CCE.Application.Community;
using CCE.Application.Community.Public.Dtos;
using CCE.Domain.Community;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Community;

public sealed class ReplyRepository : IReplyRepository
{
    private readonly CceDbContext _db;

    public ReplyRepository(CceDbContext db) => _db = db;

    public Task<Post?> GetPostAsync(Guid postId, CancellationToken ct)
        => _db.Posts.FirstOrDefaultAsync(p => p.Id == postId, ct);

    public Task<PostReply?> GetParentAsync(Guid replyId, CancellationToken ct)
        => _db.PostReplies.FirstOrDefaultAsync(r => r.Id == replyId, ct);

    public void AddReply(PostReply reply) => _db.PostReplies.Add(reply);

    public void AddMention(Mention mention) => _db.Mentions.Add(mention);

    public async Task<IReadOnlyList<Guid>> FilterVisibleUsersAsync(
        Guid communityId, IReadOnlyList<Guid> userIds, CancellationToken ct)
    {
        if (userIds.Count == 0) return System.Array.Empty<Guid>();

        var isPublic = await _db.Communities
            .Where(c => c.Id == communityId)
            .Select(c => c.Visibility == CommunityVisibility.Public)
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);

        if (isPublic)
        {
            return await _db.Users.Where(u => userIds.Contains(u.Id))
                .Select(u => u.Id).ToListAsync(ct).ConfigureAwait(false);
        }

        return await _db.CommunityMemberships
            .Where(m => m.CommunityId == communityId && userIds.Contains(m.UserId))
            .Select(m => m.UserId).Distinct().ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<MentionableUserDto>> SearchMentionableAsync(
        Guid communityId, Guid currentUserId, string q, int limit, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q)) return System.Array.Empty<MentionableUserDto>();

        var pattern = $"%{q}%";

        // Tier 1: users the current user follows whose name matches
        var tier1 = await _db.UserFollows
            .Where(f => f.FollowerId == currentUserId)
            .Join(_db.Users, f => f.FollowedId, u => u.Id, (_, u) => u)
            .Where(u => EF.Functions.Like(u.FirstName + " " + u.LastName, pattern))
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .Take(limit)
            .Select(u => new MentionableUserDto(
                u.Id,
                u.FirstName + " " + u.LastName,
                u.AvatarUrl,
                true,
                false))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var remaining = limit - tier1.Count;
        if (remaining <= 0) return tier1;

        var tier1Ids = tier1.Select(u => u.UserId).ToHashSet();

        // Tier 2: community members not already in Tier 1 whose name matches
        var tier2 = await _db.CommunityMemberships
            .Where(m => m.CommunityId == communityId && !tier1Ids.Contains(m.UserId))
            .Join(_db.Users, m => m.UserId, u => u.Id, (_, u) => u)
            .Where(u => EF.Functions.Like(u.FirstName + " " + u.LastName, pattern))
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .Take(remaining)
            .Select(u => new MentionableUserDto(
                u.Id,
                u.FirstName + " " + u.LastName,
                u.AvatarUrl,
                false,
                true))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return [..tier1, ..tier2];
    }
}
