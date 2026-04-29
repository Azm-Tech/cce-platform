using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Queries.GetResourceCategoryById;

public sealed record GetResourceCategoryByIdQuery(System.Guid Id) : IRequest<ResourceCategoryDto?>;
