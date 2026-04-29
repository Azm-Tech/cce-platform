using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Commands.UpdateResourceCategory;

public sealed record UpdateResourceCategoryCommand(
    System.Guid Id,
    string NameAr,
    string NameEn,
    int OrderIndex,
    bool IsActive) : IRequest<ResourceCategoryDto?>;
