using CCE.Application.Community;
using CCE.Domain.Community;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Community;

public sealed class CommunityModerationService : ICommunityModerationService
{
    private readonly CceDbContext _db;

    public CommunityModerationService(CceDbContext db)
    {
        _db = db;
    }

    public async Task<Post?> FindPostAsync(System.Guid id, CancellationToken ct)
    {
        return await _db.Posts.FirstOrDefaultAsync(p => p.Id == id, ct).ConfigureAwait(false);
    }

    public async Task UpdatePostAsync(Post post, CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<PostReply?> FindReplyAsync(System.Guid id, CancellationToken ct)
    {
        return await _db.PostReplies.FirstOrDefaultAsync(r => r.Id == id, ct).ConfigureAwait(false);
    }

    public async Task UpdateReplyAsync(PostReply reply, CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
