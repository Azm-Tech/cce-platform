using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using MediatR;

namespace CCE.Application.Community.Public.Queries.GetMyTopics;

/// <summary>Returns topics followed by the authenticated user, with post counts and pagination.</summary>
public sealed record GetMyTopicsQuery(
    string? Search,
    int Page,
    int PageSize) : IRequest<Response<PagedResult<MyTopicDto>>>;
