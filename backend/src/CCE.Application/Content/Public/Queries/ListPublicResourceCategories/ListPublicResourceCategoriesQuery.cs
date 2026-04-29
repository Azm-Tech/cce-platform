using CCE.Application.Content.Public.Dtos;
using MediatR;

namespace CCE.Application.Content.Public.Queries.ListPublicResourceCategories;

public sealed record ListPublicResourceCategoriesQuery() : IRequest<System.Collections.Generic.IReadOnlyList<PublicResourceCategoryDto>>;
