namespace CCE.Application.Content.Public.Dtos;

public sealed record PublicNewsDto(
    System.Guid Id,
    string TitleAr,
    string TitleEn,
    string ContentAr,
    string ContentEn,
    string Slug,
    string? FeaturedImageUrl,
    System.DateTimeOffset PublishedOn,
    bool IsFeatured);
