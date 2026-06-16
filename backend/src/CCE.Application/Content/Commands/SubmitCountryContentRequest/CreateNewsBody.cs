namespace CCE.Application.Content.Commands.SubmitCountryContentRequest;

public sealed record CreateNewsBody(
    string TitleAr,
    string TitleEn,
    string ContentAr,
    string ContentEn,
    System.Guid? FeaturedImageAssetId,
    System.Guid TopicId,
    System.Guid? KnowledgeLevelId = null,
    System.Guid? JobSectorId = null) : ContentBody;
