namespace CCE.Application.Media.Dtos;

public sealed record MediaFileDto(
    System.Guid Id,
    string StorageKey,
    string Url,
    string OriginalFileName,
    string MimeType,
    long SizeBytes,
    string? TitleAr,
    string? TitleEn,
    string? DescriptionAr,
    string? DescriptionEn,
    string? AltTextAr,
    string? AltTextEn,
    System.Guid UploadedById,
    System.DateTimeOffset UploadedOn);
