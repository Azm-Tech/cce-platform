using CCE.Application.Common;
using MediatR;

namespace CCE.Application.Content.Commands.SubmitCountryNewsRequest;

public sealed record SubmitCountryNewsRequestCommand(
    System.Guid CountryId,
    string TitleAr,
    string TitleEn,
    string ContentAr,
    string ContentEn,
    System.Guid TopicId,
    System.Guid? FeaturedImageAssetId) : IRequest<Response<System.Guid>>;
