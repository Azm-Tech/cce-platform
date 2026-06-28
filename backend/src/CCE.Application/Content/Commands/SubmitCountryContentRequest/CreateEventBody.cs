namespace CCE.Application.Content.Commands.SubmitCountryContentRequest;

public sealed record CreateEventBody(
    string TitleAr,
    string TitleEn,
    string DescriptionAr,
    string DescriptionEn,
    System.DateTimeOffset StartsOn,
    System.DateTimeOffset EndsOn,
    string? LocationAr,
    string? LocationEn,
    string? OnlineMeetingUrl,
    System.Guid? FeaturedImageAssetId,
    System.Guid TopicId,
    System.Guid? KnowledgeLevelId = null,
    System.Guid? JobSectorId = null) : ContentBody;
