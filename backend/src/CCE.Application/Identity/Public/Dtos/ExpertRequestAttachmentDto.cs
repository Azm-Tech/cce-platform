using CCE.Domain.Identity;

namespace CCE.Application.Identity.Public.Dtos;

public sealed record ExpertRequestAttachmentDto(
    System.Guid Id,
    System.Guid AssetFileId,
    ExpertRequestAttachmentType AttachmentType,
    System.DateTimeOffset UploadedAt);
