using CCE.Domain.Common;

namespace CCE.Domain.Community;

/// <summary>
/// A media or document attachment on a post, pointing at a scanned <c>AssetFile</c> (D5). Display
/// metadata (caption/alt) lives in <see cref="MetadataJson"/> (opaque blob, §3). NOT audited.
/// </summary>
public sealed class PostAttachment : Entity<System.Guid>
{
    private PostAttachment(System.Guid id, System.Guid postId, System.Guid assetFileId,
        AttachmentKind kind, int sortOrder, string? metadataJson) : base(id)
    {
        PostId = postId;
        AssetFileId = assetFileId;
        Kind = kind;
        SortOrder = sortOrder;
        MetadataJson = metadataJson;
    }

    public System.Guid PostId { get; private set; }
    public System.Guid AssetFileId { get; private set; }
    public AttachmentKind Kind { get; private set; }
    public int SortOrder { get; private set; }
    public string? MetadataJson { get; private set; }

    public static PostAttachment Create(System.Guid postId, System.Guid assetFileId,
        AttachmentKind kind, int sortOrder, string? metadataJson)
    {
        if (postId == System.Guid.Empty) throw new DomainException("PostId is required.");
        if (assetFileId == System.Guid.Empty) throw new DomainException("AssetFileId is required.");
        if (sortOrder < 0) throw new DomainException("SortOrder must be non-negative.");
        return new PostAttachment(System.Guid.NewGuid(), postId, assetFileId, kind, sortOrder, metadataJson);
    }
}
