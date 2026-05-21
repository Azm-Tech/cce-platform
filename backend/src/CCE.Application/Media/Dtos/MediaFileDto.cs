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
    System.DateTimeOffset UploadedOn)
{
    internal static MediaFileDto FromEntity(CCE.Domain.Media.MediaFile entity) => new(
        entity.Id, entity.StorageKey, entity.Url,
        entity.OriginalFileName, entity.MimeType, entity.SizeBytes,
        entity.TitleAr, entity.TitleEn,
        entity.DescriptionAr, entity.DescriptionEn,
        entity.AltTextAr, entity.AltTextEn,
        entity.UploadedById, entity.UploadedOn);
}
