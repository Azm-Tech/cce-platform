using CCE.Application.Community;
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
}
