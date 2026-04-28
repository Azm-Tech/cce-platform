using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Dtos;
using MediatR;

namespace CCE.Application.Identity.Queries.ListUsers;

public sealed class ListUsersQueryHandler : IRequestHandler<ListUsersQuery, PagedResult<UserListItemDto>>
{
    private readonly ICceDbContext _db;

    public ListUsersQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

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
            var roleName = request.Role.Trim();
            query = from u in query
                    join ur in _db.UserRoles on u.Id equals ur.UserId
                    join r in _db.Roles on ur.RoleId equals r.Id
                    where r.Name == roleName
                    select u;
        }

        query = query.OrderBy(u => u.UserName);

        var page = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken).ConfigureAwait(false);

        if (page.Items.Count == 0)
        {
            return new PagedResult<UserListItemDto>(System.Array.Empty<UserListItemDto>(), page.Page, page.PageSize, page.Total);
        }

        var userIds = page.Items.Select(u => u.Id).ToList();
        var pairs =
            from ur in _db.UserRoles
            join r in _db.Roles on ur.RoleId equals r.Id
            where userIds.Contains(ur.UserId) && r.Name != null
            select new RoleAssignmentRow(ur.UserId, r.Name!);
        var pairsList = await pairs.ToListAsyncEither(cancellationToken).ConfigureAwait(false);

        var rolesByUser = pairsList
            .GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(p => p.RoleName).ToList());

        var now = System.DateTimeOffset.UtcNow;
        var items = page.Items.Select(u => new UserListItemDto(
            u.Id,
            u.Email,
            u.UserName,
            rolesByUser.TryGetValue(u.Id, out var roles) ? roles : System.Array.Empty<string>(),
            !u.LockoutEnabled || u.LockoutEnd is null || u.LockoutEnd < now)).ToList();

        return new PagedResult<UserListItemDto>(items, page.Page, page.PageSize, page.Total);
    }

    private sealed record RoleAssignmentRow(System.Guid UserId, string RoleName);
}
