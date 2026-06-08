using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using MediatR;

namespace CCE.Application.Community.Public.Queries.ListPublicCommunities;

public sealed record ListPublicCommunitiesQuery(int Page, int PageSize)
    : IRequest<Response<PagedResult<CommunityDto>>>;
