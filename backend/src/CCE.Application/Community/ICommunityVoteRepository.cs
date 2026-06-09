using CCE.Domain.Community;

namespace CCE.Application.Community;

/// <summary>
/// Write-side repository for the voting aggregate paths. Fetches tracked aggregates and
/// stages vote rows; the unit-of-work commit is the caller's <c>ICceDbContext.SaveChangesAsync</c>
/// (§A.1 — repos fetch, the context commits). No <c>SaveChanges</c> happens here.
/// </summary>
public interface ICommunityVoteRepository
{
    Task<Post?> GetPostAsync(Guid postId, CancellationToken ct);
    Task<PostVote?> FindPostVoteAsync(Guid postId, Guid userId, CancellationToken ct);
    void AddPostVote(PostVote vote);
    void RemovePostVote(PostVote vote);

    Task<PostReply?> GetReplyAsync(Guid replyId, CancellationToken ct);
    Task<ReplyVote?> FindReplyVoteAsync(Guid replyId, Guid userId, CancellationToken ct);
    void AddReplyVote(ReplyVote vote);
    void RemoveReplyVote(ReplyVote vote);
}
