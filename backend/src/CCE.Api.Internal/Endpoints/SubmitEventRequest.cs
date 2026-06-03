namespace CCE.Api.Internal.Endpoints;

public sealed record SubmitEventRequest(
    System.Guid CountryId,
    string TitleAr,
    string TitleEn,
    string DescriptionAr,
    string DescriptionEn,
    System.Guid TopicId,
    System.DateTimeOffset StartsOn,
    System.DateTimeOffset EndsOn,
    string? LocationAr,
    string? LocationEn,
    string? OnlineMeetingUrl);
