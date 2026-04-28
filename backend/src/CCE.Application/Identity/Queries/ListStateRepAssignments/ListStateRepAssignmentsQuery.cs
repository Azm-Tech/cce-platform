using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Dtos;
using MediatR;

namespace CCE.Application.Identity.Queries.ListStateRepAssignments;

/// <summary>
/// Paged list of state-representative assignments. Default page=1, pageSize=20.
/// <c>Active</c> defaults to true (only active assignments). false includes revoked.
/// <c>UserId</c> / <c>CountryId</c> filter by exact match.
/// </summary>
public sealed record ListStateRepAssignmentsQuery(
    int Page = 1,
    int PageSize = 20,
    System.Guid? UserId = null,
    System.Guid? CountryId = null,
    bool Active = true) : IRequest<PagedResult<StateRepAssignmentDto>>;
