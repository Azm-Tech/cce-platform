using CCE.Application.Common;
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
    IReadOnlyList<System.Guid> CountryIds,
    System.Guid? KnowledgeLevelId = null,
    System.Guid? JobSectorId = null) : IRequest<Response<System.Guid>>;
