using CCE.Domain.Content;

namespace CCE.Application.Content.Dtos;

public sealed record AssetFileDto(
    System.Guid Id,
    string Url,
    string OriginalFileName,
    long SizeBytes,
    string MimeType,
    System.Guid UploadedById,
    System.DateTimeOffset UploadedOn,
    VirusScanStatus VirusScanStatus,
    System.DateTimeOffset? ScannedOn);
