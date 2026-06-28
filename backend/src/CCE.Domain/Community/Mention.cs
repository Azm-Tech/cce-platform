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
        System.Guid postId, System.Guid communityId, string snippet,
        System.Guid mentionedUserId, System.Guid mentionedByUserId, System.DateTimeOffset createdOn) : base(id)
    {
        SourceType = sourceType;
        SourceId = sourceId;
        PostId = postId;
        CommunityId = communityId;
        Snippet = snippet;
        MentionedUserId = mentionedUserId;
        MentionedByUserId = mentionedByUserId;
        CreatedOn = createdOn;
    }

    public MentionSourceType SourceType { get; private set; }
    public System.Guid SourceId { get; private set; }

    /// <summary>Always the root post — same as SourceId for post mentions, parent post for reply mentions.</summary>
    public System.Guid PostId { get; private set; }

    /// <summary>Denormalized community id — avoids joining through Post for every mention query.</summary>
    public System.Guid CommunityId { get; private set; }

    /// <summary>First 120 chars of the source content, stored at write time to avoid runtime joins.</summary>
    public string Snippet { get; private set; } = string.Empty;

    public System.Guid MentionedUserId { get; private set; }
    public System.Guid MentionedByUserId { get; private set; }
    public System.DateTimeOffset CreatedOn { get; private set; }

    public static Mention Create(
        MentionSourceType sourceType,
        System.Guid sourceId,
        System.Guid postId,
        System.Guid communityId,
        string snippet,
        System.Guid mentionedUserId,
        System.Guid mentionedByUserId,
        ISystemClock clock)
    {
        if (sourceId == System.Guid.Empty) throw new DomainException("SourceId is required.");
        if (postId == System.Guid.Empty) throw new DomainException("PostId is required.");
        if (communityId == System.Guid.Empty) throw new DomainException("CommunityId is required.");
        if (mentionedUserId == System.Guid.Empty) throw new DomainException("MentionedUserId is required.");
        if (mentionedByUserId == System.Guid.Empty) throw new DomainException("MentionedByUserId is required.");
        var safeSnippet = snippet.Length > 120 ? snippet[..120] : snippet;
        return new Mention(System.Guid.NewGuid(), sourceType, sourceId, postId, communityId, safeSnippet,
            mentionedUserId, mentionedByUserId, clock.UtcNow);
    }
}
