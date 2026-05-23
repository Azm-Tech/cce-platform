namespace CCE.Application.Community;

public interface ICommunityReadService
{
    /// <summary>
    /// Returns distinct user IDs who follow the given topic,
    /// optionally excluding a specific user (e.g., the author).
    /// </summary>
    Task<IReadOnlyList<System.Guid>> GetTopicFollowerIdsAsync(
        System.Guid topicId,
        System.Guid? excludeUserId,
        CancellationToken ct);
}
