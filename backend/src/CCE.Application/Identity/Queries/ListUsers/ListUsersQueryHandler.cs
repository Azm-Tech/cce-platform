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

    public async Task<PagedResult<UserListItemDto>> Handle(
     ListUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Users.Where(u => !u.IsDeleted);

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
            // Distinct prevents duplicates when a user has the role assigned more than once
            query = query
                .Where(u => _db.UserRoles
                    .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                    .Any(x => x.UserId == u.Id && x.Name == role));
        }

        query = query.OrderBy(u => u.UserName);

        // Single projection — roles are fetched in the same query, no second round-trip
        var projected = query.Select(u => new UserListItemDto(
            u.Id,
            u.Email,
            u.UserName,
            _db.UserRoles
                .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .Where(x => x.UserId == u.Id && x.Name != null)
                .Select(x => x.Name!)
                .ToList(),
            u.Status == Domain.Identity.UserStatus.Active));

        return await projected
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);
    }
}
