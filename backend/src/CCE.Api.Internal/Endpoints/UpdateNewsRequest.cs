namespace CCE.Api.Internal.Endpoints;

public sealed record UpdateNewsRequest(
    string TitleAr, string TitleEn, string ContentAr, string ContentEn,
    System.Guid TopicId, string? FeaturedImageUrl,
    System.Collections.Generic.List<System.Guid>? TagIds = null);
