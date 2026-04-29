using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Commands.CreateResourceCategory;

public sealed record CreateResourceCategoryCommand(
    string NameAr,
    string NameEn,
    string Slug,
    System.Guid? ParentId,
    int OrderIndex) : IRequest<ResourceCategoryDto>;
