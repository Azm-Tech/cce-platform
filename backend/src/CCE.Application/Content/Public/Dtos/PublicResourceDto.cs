using CCE.Domain.Content;

namespace CCE.Application.Content.Public.Dtos;

public sealed record PublicResourceDto(
    System.Guid Id,
    string TitleAr,
    string TitleEn,
    string DescriptionAr,
    string DescriptionEn,
    ResourceType ResourceType,
    string ResourceTypeAr,
    System.Guid CategoryId,
    string CategoryNameAr,
    string CategoryNameEn,
    System.Guid AssetFileId,
    string AssetFileName,
    IReadOnlyList<System.Guid> CountryIds,
    IReadOnlyList<string> CountryNames,
    string PublishedBy,
    System.DateTimeOffset PublishedOn,
    long ViewCount);
