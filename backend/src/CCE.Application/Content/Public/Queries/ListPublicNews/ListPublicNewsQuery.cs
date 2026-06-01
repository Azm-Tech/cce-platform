using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using MediatR;

namespace CCE.Application.Content.Public.Queries.ListPublicNews;

public sealed record ListPublicNewsQuery(
    int Page = 1,
    int PageSize = 20,
    bool? IsFeatured = null,
    System.Guid? TopicId = null,
    string? TopicSlug = null) : IRequest<Response<PagedResult<PublicNewsDto>>>;
