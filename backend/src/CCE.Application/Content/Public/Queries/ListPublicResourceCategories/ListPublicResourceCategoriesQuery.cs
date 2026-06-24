using CCE.Application.Common;
using CCE.Application.Content.Public.Dtos;
using MediatR;

namespace CCE.Application.Content.Public.Queries.ListPublicResourceCategories;

public sealed record ListPublicResourceCategoriesQuery() : IRequest<Response<System.Collections.Generic.IReadOnlyList<PublicResourceCategoryDto>>>;
