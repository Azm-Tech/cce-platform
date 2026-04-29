using CCE.Domain.Community;

namespace CCE.Application.Community;

public interface ICommunityWriteService
{
    Task SavePostAsync(Post post, CancellationToken ct);
    Task SaveReplyAsync(PostReply reply, CancellationToken ct);
    Task SaveRatingAsync(PostRating rating, CancellationToken ct);
    Task<Post?> FindPostAsync(Guid id, CancellationToken ct);
    Task<PostReply?> FindReplyAsync(Guid id, CancellationToken ct);
    Task UpdatePostAsync(Post post, CancellationToken ct);
    Task UpdateReplyAsync(PostReply reply, CancellationToken ct);
    Task SaveFollowAsync<T>(T follow, CancellationToken ct) where T : class;
    Task<TopicFollow?> FindTopicFollowAsync(Guid topicId, Guid userId, CancellationToken ct);
    Task<UserFollow?> FindUserFollowAsync(Guid followerId, Guid followedId, CancellationToken ct);
    Task<PostFollow?> FindPostFollowAsync(Guid postId, Guid userId, CancellationToken ct);
    Task<bool> RemoveTopicFollowAsync(Guid topicId, Guid userId, CancellationToken ct);
    Task<bool> RemoveUserFollowAsync(Guid followerId, Guid followedId, CancellationToken ct);
    Task<bool> RemovePostFollowAsync(Guid postId, Guid userId, CancellationToken ct);
}
