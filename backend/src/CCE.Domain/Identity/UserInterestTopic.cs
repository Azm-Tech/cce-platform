namespace CCE.Domain.Identity;

public sealed class UserInterestTopic
{
    public System.Guid UserId { get; init; }

    public User User { get; init; } = null!;

    public System.Guid InterestTopicId { get; init; }

    public InterestTopic InterestTopic { get; init; } = null!;
}
