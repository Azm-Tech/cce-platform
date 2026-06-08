using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using MediatR;

namespace CCE.Application.Community.Queries.ListJoinRequests;

/// <summary>Admin/moderator queue of pending join requests for a community.</summary>
public sealed record ListJoinRequestsQuery(Guid CommunityId, int Page, int PageSize)
    : IRequest<Response<PagedResult<JoinRequestDto>>>;
