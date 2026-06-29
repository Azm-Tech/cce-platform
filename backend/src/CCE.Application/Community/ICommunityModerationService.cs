using CCE.Domain.Community;

namespace CCE.Application.Community;

public interface ICommunityModerationService
{
    Task<Post?>      FindPostAsync (System.Guid id, CancellationToken ct);
    Task<PostReply?> FindReplyAsync(System.Guid id, CancellationToken ct);
    Task ReIndexPostAsync (System.Guid postId,  CancellationToken ct);
    Task ReIndexReplyAsync(System.Guid replyId, CancellationToken ct);
}
