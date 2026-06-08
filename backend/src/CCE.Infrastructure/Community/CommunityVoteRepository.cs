using CCE.Application.Community;
using CCE.Domain.Community;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Community;

/// <summary>
/// EF implementation of <see cref="ICommunityVoteRepository"/>. Returns tracked entities so the
/// caller's <c>ICceDbContext.SaveChangesAsync</c> persists the mutations as one unit of work.
/// </summary>
public sealed class CommunityVoteRepository : ICommunityVoteRepository
{
    private readonly CceDbContext _db;

    public CommunityVoteRepository(CceDbContext db) => _db = db;

    public Task<Post?> GetPostAsync(Guid postId, CancellationToken ct)
        => _db.Posts.FirstOrDefaultAsync(p => p.Id == postId, ct);

    public Task<PostVote?> FindPostVoteAsync(Guid postId, Guid userId, CancellationToken ct)
        => _db.PostVotes.FirstOrDefaultAsync(v => v.PostId == postId && v.UserId == userId, ct);

    public void AddPostVote(PostVote vote) => _db.PostVotes.Add(vote);

    public void RemovePostVote(PostVote vote) => _db.PostVotes.Remove(vote);

    public Task<PostReply?> GetReplyAsync(Guid replyId, CancellationToken ct)
        => _db.PostReplies.FirstOrDefaultAsync(r => r.Id == replyId, ct);

    public Task<ReplyVote?> FindReplyVoteAsync(Guid replyId, Guid userId, CancellationToken ct)
        => _db.ReplyVotes.FirstOrDefaultAsync(v => v.ReplyId == replyId && v.UserId == userId, ct);

    public void AddReplyVote(ReplyVote vote) => _db.ReplyVotes.Add(vote);

    public void RemoveReplyVote(ReplyVote vote) => _db.ReplyVotes.Remove(vote);
}
