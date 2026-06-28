namespace CCE.Api.Internal.Endpoints;

public sealed record UpdateEventRequest(
    string TitleAr, string TitleEn,
    string DescriptionAr, string DescriptionEn,
    string? LocationAr, string? LocationEn,
    string? OnlineMeetingUrl, string? FeaturedImageUrl,
    System.Guid TopicId,
    System.Collections.Generic.List<System.Guid>? TagIds = null,
    System.Guid? KnowledgeLevelId = null,
    System.Guid? JobSectorId = null);
