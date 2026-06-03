using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Content.Commands.SubmitCountryEventRequest;

public sealed record SubmitCountryEventRequestCommand(
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
    string? OnlineMeetingUrl) : IRequest<Response<System.Guid>>;
