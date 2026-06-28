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

    /// <summary>Distinct user IDs who follow the given community, optionally excluding one user.</summary>
    Task<IReadOnlyList<System.Guid>> GetCommunityFollowerIdsAsync(
        System.Guid communityId,
        System.Guid? excludeUserId,
        CancellationToken ct);

    /// <summary>Moderator user IDs of the given community.</summary>
    Task<IReadOnlyList<System.Guid>> GetCommunityModeratorIdsAsync(
        System.Guid communityId,
        CancellationToken ct);
}
