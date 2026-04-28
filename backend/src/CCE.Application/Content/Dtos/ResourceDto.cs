using CCE.Domain.Content;

namespace CCE.Application.Content.Dtos;

public sealed record ResourceDto(
    System.Guid Id,
    string TitleAr,
    string TitleEn,
    string DescriptionAr,
    string DescriptionEn,
    ResourceType ResourceType,
    System.Guid CategoryId,
    System.Guid? CountryId,
    System.Guid UploadedById,
    System.Guid AssetFileId,
    System.DateTimeOffset? PublishedOn,
    long ViewCount,
    bool IsCenterManaged,
    bool IsPublished,
    string RowVersion);
