using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Dtos;
using MediatR;

namespace CCE.Application.Identity.Queries.ListUsers;

/// <summary>
/// Paged + filtered list of users. Default page=1, pageSize=20.
/// <c>Search</c> matches case-insensitive against UserName or Email (LIKE %term%).
/// <c>Role</c> filters to users assigned the given role name (exact match on
/// the AspNetRoles.Name column).
/// </summary>
public sealed record ListUsersQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Role = null) : IRequest<PagedResult<UserListItemDto>>;
