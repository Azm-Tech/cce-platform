using CCE.Domain.Content;
using CCE.Domain.Country;

namespace CCE.Api.Common.Requests;

public sealed record SubmitContentRequest(
    ContentType Type,
    System.Guid CountryId,
    string TitleAr,
    string TitleEn,
    string DescriptionAr,
    string DescriptionEn,
    ResourceType? ResourceType = null,
    System.Guid? AssetFileId = null,
    System.Guid? TopicId = null,
    System.Guid? FeaturedImageAssetId = null,
    System.DateTimeOffset? StartsOn = null,
    System.DateTimeOffset? EndsOn = null,
    string? LocationAr = null,
    string? LocationEn = null,
    string? OnlineMeetingUrl = null);
