namespace CCE.Api.Internal.Endpoints;

public sealed record CreateNewsRequest(
    string TitleAr, string TitleEn, string ContentAr, string ContentEn,
    System.Guid TopicId, string? FeaturedImageUrl);
