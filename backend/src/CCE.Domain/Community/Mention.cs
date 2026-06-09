using CCE.Domain.Common;

namespace CCE.Domain.Community;

/// <summary>
/// An @mention of a user inside a post or reply (D8). Polymorphic via
/// <see cref="SourceType"/> + <see cref="SourceId"/>. Unique per (source, mentioned user);
/// drives the mention notification. NOT audited.
/// </summary>
public sealed class Mention : Entity<System.Guid>
{
    private Mention(System.Guid id, MentionSourceType sourceType, System.Guid sourceId,
        System.Guid mentionedUserId, System.Guid mentionedByUserId, System.DateTimeOffset createdOn) : base(id)
    {
        SourceType = sourceType;
        SourceId = sourceId;
        MentionedUserId = mentionedUserId;
        MentionedByUserId = mentionedByUserId;
        CreatedOn = createdOn;
    }

    public MentionSourceType SourceType { get; private set; }
    public System.Guid SourceId { get; private set; }
    public System.Guid MentionedUserId { get; private set; }
    public System.Guid MentionedByUserId { get; private set; }
    public System.DateTimeOffset CreatedOn { get; private set; }

    public static Mention Create(MentionSourceType sourceType, System.Guid sourceId,
        System.Guid mentionedUserId, System.Guid mentionedByUserId, ISystemClock clock)
    {
        if (sourceId == System.Guid.Empty) throw new DomainException("SourceId is required.");
        if (mentionedUserId == System.Guid.Empty) throw new DomainException("MentionedUserId is required.");
        if (mentionedByUserId == System.Guid.Empty) throw new DomainException("MentionedByUserId is required.");
        return new Mention(System.Guid.NewGuid(), sourceType, sourceId, mentionedUserId, mentionedByUserId, clock.UtcNow);
    }
}
