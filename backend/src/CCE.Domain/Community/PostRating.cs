using CCE.Domain.Common;

namespace CCE.Domain.Community;

/// <summary>
/// One user's star rating on a post (1–5). Uniqueness is enforced by Phase 08 unique
/// index on (PostId, UserId). NOT audited (high-volume association — spec §4.11).
/// </summary>
public sealed class PostRating : Entity<System.Guid>
{
    private PostRating(System.Guid id, System.Guid postId, System.Guid userId,
        int stars, System.DateTimeOffset ratedOn) : base(id)
    {
        PostId = postId; UserId = userId;
        Stars = stars; RatedOn = ratedOn;
    }

    public System.Guid PostId { get; private set; }
    public System.Guid UserId { get; private set; }
    public int Stars { get; private set; }
    public System.DateTimeOffset RatedOn { get; private set; }

    public static PostRating Rate(System.Guid postId, System.Guid userId, int stars, ISystemClock clock)
    {
        if (postId == System.Guid.Empty) throw new DomainException("PostId is required.");
        if (userId == System.Guid.Empty) throw new DomainException("UserId is required.");
        if (stars < 1 || stars > 5)
        {
            throw new DomainException($"Stars must be between 1 and 5 (got {stars}).");
        }
        return new PostRating(System.Guid.NewGuid(), postId, userId, stars, clock.UtcNow);
    }

    public void Update(int stars, ISystemClock clock)
    {
        if (stars < 1 || stars > 5)
        {
            throw new DomainException($"Stars must be between 1 and 5 (got {stars}).");
        }
        Stars = stars;
        RatedOn = clock.UtcNow;
    }
}
