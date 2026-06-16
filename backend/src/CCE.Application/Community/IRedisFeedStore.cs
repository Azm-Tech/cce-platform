namespace CCE.Application.Community;

/// <summary>
/// Redis-backed read-model store for community feeds and hot leaderboards. The SQL database
/// remains the source of truth; Redis carries hot derived data only (§11).
///
/// <para>
/// Keys are prefixed <c>feed:</c>, <c>post:</c>, <c>hot:</c>, and <c>notif:</c> per the
/// Spring 9 architecture guide.
/// </para>
/// </summary>
public interface IRedisFeedStore
{
    // ─── Feed (merged timeline) ───
    Task AddToUserFeedAsync(Guid userId, Guid postId, DateTimeOffset publishedOn, CancellationToken ct = default);
    Task AddToCommunityFeedAsync(Guid communityId, Guid postId, DateTimeOffset publishedOn, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetUserFeedAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetCommunityFeedAsync(Guid communityId, int page, int pageSize, CancellationToken ct = default);
    Task<long> GetCommunityFeedCountAsync(Guid communityId, CancellationToken ct = default);
    Task<long> GetHotLeaderboardCountAsync(Guid communityId, CancellationToken ct = default);
    Task RemoveFromFeedAsync(Guid userId, Guid postId, CancellationToken ct = default);

    // ─── Post hot counters ───
    Task IncrementPostVotesAsync(Guid postId, int upDelta, int downDelta, CancellationToken ct = default);
    Task<(int Upvotes, int Downvotes)> GetPostVotesAsync(Guid postId, CancellationToken ct = default);
    Task SetPostMetaAsync(Guid postId, int upvotes, int downvotes, double score, int replyCount, CancellationToken ct = default);
    Task<PostMeta?> GetPostMetaAsync(Guid postId, CancellationToken ct = default);

    // ─── Hot leaderboards ───
    Task AddToHotLeaderboardAsync(Guid communityId, Guid postId, double score, CancellationToken ct = default);
    Task RemoveFromHotLeaderboardAsync(Guid communityId, Guid postId, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetHotPostsAsync(Guid communityId, int topN, CancellationToken ct = default);

    // ─── Notifications ───
    Task IncrementNotificationCountAsync(Guid userId, int delta = 1, CancellationToken ct = default);
    Task<int> GetNotificationCountAsync(Guid userId, CancellationToken ct = default);
    Task ResetNotificationCountAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>Redis-stored hot metadata for a post (not the full SQL row).</summary>
public sealed record PostMeta(
    int Upvotes,
    int Downvotes,
    double Score,
    int ReplyCount);
