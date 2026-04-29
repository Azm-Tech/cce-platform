using CCE.Domain.Content;

namespace CCE.Application.Content.Public.Dtos;

public sealed record PublicResourceDto(
    System.Guid Id,
    string TitleAr,
    string TitleEn,
    string DescriptionAr,
    string DescriptionEn,
    ResourceType ResourceType,
    System.Guid CategoryId,
    System.Guid? CountryId,
    System.Guid AssetFileId,
    System.DateTimeOffset PublishedOn,
    long ViewCount);
