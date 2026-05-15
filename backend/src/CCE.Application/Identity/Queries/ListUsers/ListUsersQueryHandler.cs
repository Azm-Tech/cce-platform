using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Dtos;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace CCE.Application.Identity.Queries.ListUsers;

public sealed class ListUsersQueryHandler : IRequestHandler<ListUsersQuery, PagedResult<UserListItemDto>>
{
    private readonly ICceDbContext _db;

    public ListUsersQueryHandler(ICceDbContext db) => _db = db;

    public async Task<PagedResult<UserListItemDto>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(u =>
                (u.UserName != null && u.UserName.Contains(term)) ||
                (u.Email != null && u.Email.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var role = request.Role.Trim();
            query = from u in query
                    join ur in _db.UserRoles on u.Id equals ur.UserId
                    join r in _db.Roles on ur.RoleId equals r.Id
                    where r.Name == role
                    select u;
        }

        query = query.OrderBy(u => u.UserName);

        var paged = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken).ConfigureAwait(false);

        if (paged.Items.Count == 0)
        {
            return new PagedResult<UserListItemDto>(
                Array.Empty<UserListItemDto>(), paged.Page, paged.PageSize, paged.Total);
        }

        var userIds = paged.Items.Select(u => u.Id).ToList();
        var pairs =
            from ur in _db.UserRoles
            join r in _db.Roles on ur.RoleId equals r.Id
            where userIds.Contains(ur.UserId) && r.Name != null
            select new RoleAssignmentRow(ur.UserId, r.Name!);
        var pairsList = await pairs.ToListAsyncEither(cancellationToken).ConfigureAwait(false);

        var rolesByUser = pairsList
            .GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(p => p.RoleName).ToList());

        var now = DateTimeOffset.UtcNow;
        var items = paged.Items.Select(u => new UserListItemDto(
            u.Id,
            u.Email,
            u.UserName,
            rolesByUser.TryGetValue(u.Id, out var roles) ? roles : Array.Empty<string>(),
            !u.LockoutEnabled || u.LockoutEnd is null || u.LockoutEnd < now)).ToList();

        return new PagedResult<UserListItemDto>(items, paged.Page, paged.PageSize, paged.Total);
    }

    private sealed record RoleAssignmentRow(Guid UserId, string RoleName);
}
