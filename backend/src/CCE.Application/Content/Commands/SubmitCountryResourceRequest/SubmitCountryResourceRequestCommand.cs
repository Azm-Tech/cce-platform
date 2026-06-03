using CCE.Application.Common;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.SubmitCountryResourceRequest;

public sealed record SubmitCountryResourceRequestCommand(
    System.Guid CountryId,
    string TitleAr,
    string TitleEn,
    string DescriptionAr,
    string DescriptionEn,
    ResourceType ResourceType,
    System.Guid AssetFileId) : IRequest<Response<System.Guid>>;
