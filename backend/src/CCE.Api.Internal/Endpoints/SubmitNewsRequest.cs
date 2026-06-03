namespace CCE.Api.Internal.Endpoints;

public sealed record SubmitNewsRequest(
    System.Guid CountryId,
    string TitleAr,
    string TitleEn,
    string ContentAr,
    string ContentEn,
    System.Guid TopicId,
    System.Guid? FeaturedImageAssetId);
