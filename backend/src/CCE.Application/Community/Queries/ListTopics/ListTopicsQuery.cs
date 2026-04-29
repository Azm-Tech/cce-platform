using CCE.Application.Common.Pagination;
using CCE.Application.Community.Dtos;
using MediatR;

namespace CCE.Application.Community.Queries.ListTopics;

public sealed record ListTopicsQuery(
    int Page = 1,
    int PageSize = 20,
    System.Guid? ParentId = null,
    bool? IsActive = null,
    string? Search = null) : IRequest<PagedResult<TopicDto>>;
