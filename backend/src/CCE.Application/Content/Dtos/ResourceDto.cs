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
    string CategoryNameAr,
    string CategoryNameEn,
    System.Guid AssetFileId,
    string AssetFileName,
    IReadOnlyList<System.Guid> CountryIds,
    IReadOnlyList<string> CountryNames,
    System.Guid UploadedById,
    System.DateTimeOffset? PublishedOn,
    long ViewCount,
    bool IsCenterManaged,
    bool IsPublished);
