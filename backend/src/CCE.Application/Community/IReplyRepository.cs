using CCE.Application.Community.Public.Dtos;
using CCE.Domain.Community;

namespace CCE.Application.Community;

/// <summary>Write-side repository for replies and their mentions (§A.1).</summary>
public interface IReplyRepository
{
    Task<Post?> GetPostAsync(Guid postId, CancellationToken ct);
    Task<PostReply?> GetParentAsync(Guid replyId, CancellationToken ct);
    void AddReply(PostReply reply);
    void AddMention(Mention mention);

    /// <summary>
    /// Returns the subset of <paramref name="userIds"/> allowed to see the community: for a public
    /// community any existing user; for a private community only its members. Drives mention gating.
    /// </summary>
    Task<IReadOnlyList<Guid>> FilterVisibleUsersAsync(Guid communityId, IReadOnlyList<Guid> userIds, CancellationToken ct);

    /// <summary>
    /// Two-tier @mention autocomplete: Tier 1 = users the caller follows (matched by name),
    /// Tier 2 = community members not in Tier 1. Short-circuits when <paramref name="q"/> is empty.
    /// </summary>
    Task<IReadOnlyList<MentionableUserDto>> SearchMentionableAsync(
        Guid communityId, Guid currentUserId, string q, int limit, CancellationToken ct);
}
