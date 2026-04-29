namespace CCE.Application.Content.Dtos;

public sealed record EventDto(
    System.Guid Id,
    string TitleAr, string TitleEn,
    string DescriptionAr, string DescriptionEn,
    System.DateTimeOffset StartsOn,
    System.DateTimeOffset EndsOn,
    string? LocationAr, string? LocationEn,
    string? OnlineMeetingUrl,
    string? FeaturedImageUrl,
    string ICalUid,
    string RowVersion);
