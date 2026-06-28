using CCE.Domain.Community;
using CCE.Domain.Content;

namespace CCE.Application.Community;

/// <summary>
/// Write-side repository for the <see cref="Post"/> aggregate (§A.1). Fetches tracked aggregates
/// (including tags) and stages adds/removes; the unit-of-work commit is the caller's
/// <c>ICceDbContext.SaveChangesAsync</c>.
/// </summary>
public interface IPostRepository
{
    Task<Post?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Lightweight scalar lookup used by the SignalR hub's <c>Subscribe</c> path — returns only the
    /// <see cref="Post.CommunityId"/> (untracked, no <c>Tags</c> include) so access checks don't pay
    /// the cost of hydrating a full trackable aggregate.
    /// </summary>
    Task<Guid?> GetCommunityIdAsync(Guid id, CancellationToken ct);

    Task<bool> TopicExistsAsync(Guid topicId, CancellationToken ct);
    Task<IReadOnlyList<Tag>> GetTagsAsync(IReadOnlyList<Guid> tagIds, CancellationToken ct);
    Task<IReadOnlyList<AssetFile>> GetAssetsAsync(IReadOnlyList<Guid> assetIds, CancellationToken ct);
    void Add(Post post);
    void Remove(Post post);
}
