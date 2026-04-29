namespace CCE.Application.Content.Dtos;

public sealed record NewsDto(
    System.Guid Id,
    string TitleAr,
    string TitleEn,
    string ContentAr,
    string ContentEn,
    string Slug,
    System.Guid AuthorId,
    string? FeaturedImageUrl,
    System.DateTimeOffset? PublishedOn,
    bool IsFeatured,
    bool IsPublished,
    string RowVersion);
