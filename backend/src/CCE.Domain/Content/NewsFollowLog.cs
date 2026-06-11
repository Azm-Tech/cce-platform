using CCE.Domain.Common;

namespace CCE.Domain.Content;

public sealed class NewsFollowLog : Entity<System.Guid>
{
    private NewsFollowLog(
        System.Guid id,
        System.Guid userId,
        System.Guid newsId,
        System.DateTimeOffset timestamp) : base(id)
    {
        UserId = userId;
        NewsId = newsId;
        Timestamp = timestamp;
    }

    public System.Guid UserId { get; private set; }
    public System.Guid NewsId { get; private set; }
    public System.DateTimeOffset Timestamp { get; private set; }

    public static NewsFollowLog Log(System.Guid userId, System.Guid newsId, ISystemClock clock)
    {
        return new NewsFollowLog(System.Guid.NewGuid(), userId, newsId, clock.UtcNow);
    }
}