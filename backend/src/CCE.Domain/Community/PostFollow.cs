using CCE.Domain.Common;

namespace CCE.Domain.Community;

public sealed class PostFollow : Entity<System.Guid>
{
    private PostFollow(System.Guid id, System.Guid postId, System.Guid userId,
        System.DateTimeOffset followedOn) : base(id)
    {
        PostId = postId; UserId = userId; FollowedOn = followedOn;
    }

    public System.Guid PostId { get; private set; }
    public System.Guid UserId { get; private set; }
    public System.DateTimeOffset FollowedOn { get; private set; }

    public static PostFollow Follow(System.Guid postId, System.Guid userId, ISystemClock clock)
    {
        if (postId == System.Guid.Empty) throw new DomainException("PostId is required.");
        if (userId == System.Guid.Empty) throw new DomainException("UserId is required.");
        return new PostFollow(System.Guid.NewGuid(), postId, userId, clock.UtcNow);
    }
}
