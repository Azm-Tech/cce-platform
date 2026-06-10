using CCE.Domain.Common;

namespace CCE.Domain.Content;

public sealed class NewsFollow : Entity<System.Guid>
{
    private NewsFollow(System.Guid id, System.Guid userId,
        System.DateTimeOffset followedOn) : base(id)
    {
        UserId = userId; FollowedOn = followedOn;
    }

    public System.Guid UserId { get; private set; }
    public System.DateTimeOffset FollowedOn { get; private set; }

    public static NewsFollow Follow(System.Guid userId, ISystemClock clock)
    {
        if (userId == System.Guid.Empty) throw new DomainException("UserId is required.");
        return new NewsFollow(System.Guid.NewGuid(), userId, clock.UtcNow);
    }
}
