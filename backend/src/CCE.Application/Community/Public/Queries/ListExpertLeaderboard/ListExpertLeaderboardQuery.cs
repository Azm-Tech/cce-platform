using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using MediatR;

namespace CCE.Application.Community.Public.Queries.ListExpertLeaderboard;

/// <summary>
/// Leaderboard of community experts ranked by contribution (published posts + replies).
/// </summary>
public sealed record ListExpertLeaderboardQuery(
    int Page,
    int PageSize) : IRequest<Response<PagedResult<ExpertLeaderboardEntryDto>>>;
