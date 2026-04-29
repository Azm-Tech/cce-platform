using CCE.Domain.Community;

namespace CCE.Application.Community;

public interface ICommunityModerationService
{
    Task<Post?> FindPostAsync(System.Guid id, CancellationToken ct);
    Task UpdatePostAsync(Post post, CancellationToken ct);
    Task<PostReply?> FindReplyAsync(System.Guid id, CancellationToken ct);
    Task UpdateReplyAsync(PostReply reply, CancellationToken ct);
}
