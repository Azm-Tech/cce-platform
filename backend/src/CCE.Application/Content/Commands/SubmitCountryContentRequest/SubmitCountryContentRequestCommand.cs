using CCE.Application.Common;
using CCE.Domain.Content;
using CCE.Domain.Country;
using MediatR;

namespace CCE.Application.Content.Commands.SubmitCountryContentRequest;

public sealed record SubmitCountryContentRequestCommand(
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
    string? OnlineMeetingUrl = null) : IRequest<Response<System.Guid>>;
