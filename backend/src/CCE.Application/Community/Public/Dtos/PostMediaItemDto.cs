using CCE.Domain.Community;

namespace CCE.Application.Community.Public.Dtos;

public sealed record PostMediaItemDto(
    System.Guid    MediaFileId,
    AttachmentKind Kind,
    string         MimeType,
    string         Url,
    long           SizeBytes,
    string         OriginalFileName,
    int            SortOrder,
    string?        MetadataJson);
