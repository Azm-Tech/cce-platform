using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Dtos;
using MediatR;

namespace CCE.Application.Identity.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDetailDto?>
{
    private readonly ICceDbContext _db;

    public GetUserByIdQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<UserDetailDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = (await _db.Users.Where(u => u.Id == request.Id).ToListAsyncEither(cancellationToken).ConfigureAwait(false))
            .SingleOrDefault();
        if (user is null)
        {
            return null;
        }

        var roleNames =
            from ur in _db.UserRoles
            join r in _db.Roles on ur.RoleId equals r.Id
            where ur.UserId == request.Id && r.Name != null
            select r.Name!;
        var roles = await roleNames.ToListAsyncEither(cancellationToken).ConfigureAwait(false);

        var now = System.DateTimeOffset.UtcNow;
        var isActive = !user.LockoutEnabled || user.LockoutEnd is null || user.LockoutEnd < now;

        return new UserDetailDto(
            user.Id,
            user.Email,
            user.UserName,
            user.LocalePreference,
            user.KnowledgeLevel,
            user.Interests,
            user.CountryId,
            user.AvatarUrl,
            roles,
            isActive);
    }
}
