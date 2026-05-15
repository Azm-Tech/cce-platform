using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Dtos;
using MediatR;

namespace CCE.Application.Identity.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDetailDto>>
{
    private readonly ICceDbContext _db;
    private readonly CCE.Application.Common.Errors _errors;

    public GetUserByIdQueryHandler(ICceDbContext db, CCE.Application.Common.Errors errors)
    {
        _db = db;
        _errors = errors;
    }

    public async Task<Result<UserDetailDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = (await _db.Users.Where(u => u.Id == request.Id).ToListAsyncEither(cancellationToken).ConfigureAwait(false))
            .SingleOrDefault();
        if (user is null)
        {
            return _errors.UserNotFound();
        }

        var roleNames =
            from ur in _db.UserRoles
            join r in _db.Roles on ur.RoleId equals r.Id
            where ur.UserId == request.Id && r.Name != null
            select r.Name!;
        var roles = await roleNames.ToListAsyncEither(cancellationToken).ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;
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
