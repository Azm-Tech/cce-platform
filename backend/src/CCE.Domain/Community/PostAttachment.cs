using CCE.Domain.Common;

namespace CCE.Domain.Community;

/// <summary>
/// A media or document attachment on a post, pointing at a <c>MediaFile</c>. Display
/// metadata (caption/alt) lives in <see cref="MetadataJson"/> (opaque blob, §3). NOT audited.
/// </summary>
public sealed class PostAttachment : Entity<System.Guid>
{
    private PostAttachment(System.Guid id, System.Guid postId, System.Guid mediaFileId,
        AttachmentKind kind, int sortOrder, string? metadataJson) : base(id)
    {
        PostId = postId;
        MediaFileId = mediaFileId;
        Kind = kind;
        SortOrder = sortOrder;
        MetadataJson = metadataJson;
    }

    public System.Guid PostId { get; private set; }
    public System.Guid MediaFileId { get; private set; }
    public AttachmentKind Kind { get; private set; }
    public int SortOrder { get; private set; }
    public string? MetadataJson { get; private set; }

    public static PostAttachment Create(System.Guid postId, System.Guid mediaFileId,
        AttachmentKind kind, int sortOrder, string? metadataJson)
    {
        if (postId == System.Guid.Empty) throw new DomainException("PostId is required.");
        if (mediaFileId == System.Guid.Empty) throw new DomainException("MediaFileId is required.");
        if (sortOrder < 0) throw new DomainException("SortOrder must be non-negative.");
        return new PostAttachment(System.Guid.NewGuid(), postId, mediaFileId, kind, sortOrder, metadataJson);
    }
}
