using CCE.Application.Content.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.CreateResource;

public sealed record CreateResourceCommand(
    string TitleAr,
    string TitleEn,
    string DescriptionAr,
    string DescriptionEn,
    ResourceType ResourceType,
    System.Guid CategoryId,
    System.Guid? CountryId,
    System.Guid AssetFileId) : IRequest<ResourceDto>;
