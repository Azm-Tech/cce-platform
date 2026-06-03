namespace CCE.Api.Internal.Endpoints;

public sealed record CreateEventRequest(
    string TitleAr, string TitleEn,
    string DescriptionAr, string DescriptionEn,
    System.DateTimeOffset StartsOn,
    System.DateTimeOffset EndsOn,
    string? LocationAr,
    string? LocationEn,
    string? OnlineMeetingUrl,
    string? FeaturedImageUrl,
    System.Guid TopicId,
    System.Collections.Generic.List<System.Guid>? TagIds = null);
