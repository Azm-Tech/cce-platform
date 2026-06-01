using CCE.Application.Common;
using CCE.Application.Content.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.UpdateResource;

public sealed record UpdateResourceCommand(
    System.Guid Id,
    string TitleAr,
    string TitleEn,
    string DescriptionAr,
    string DescriptionEn,
    ResourceType ResourceType,
    System.Guid CategoryId,
    IReadOnlyList<System.Guid> CountryIds) : IRequest<Response<ResourceDto>>;
