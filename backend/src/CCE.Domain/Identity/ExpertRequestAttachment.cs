using CCE.Domain.Common;

namespace CCE.Domain.Identity;

public sealed class ExpertRequestAttachment : Entity<System.Guid>
{
    private ExpertRequestAttachment() : base(System.Guid.NewGuid()) { }

    private ExpertRequestAttachment(
        System.Guid id,
        System.Guid expertRequestId,
        System.Guid assetFileId,
        ExpertRequestAttachmentType attachmentType,
        System.DateTimeOffset uploadedAt) : base(id)
    {
        ExpertRequestId = expertRequestId;
        AssetFileId = assetFileId;
        AttachmentType = attachmentType;
        UploadedAt = uploadedAt;
    }

    public System.Guid ExpertRequestId { get; private set; }
    public System.Guid AssetFileId { get; private set; }
    public ExpertRequestAttachmentType AttachmentType { get; private set; }
    public System.DateTimeOffset UploadedAt { get; private set; }

    internal static ExpertRequestAttachment Create(
        System.Guid expertRequestId,
        System.Guid assetFileId,
        ExpertRequestAttachmentType attachmentType,
        System.DateTimeOffset uploadedAt)
        => new(System.Guid.NewGuid(), expertRequestId, assetFileId, attachmentType, uploadedAt);
}
