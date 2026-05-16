using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Dtos;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Identity.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Response<UserDetailDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetUserByIdQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<UserDetailDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = (await _db.Users.Where(u => u.Id == request.Id).ToListAsyncEither(cancellationToken).ConfigureAwait(false))
            .SingleOrDefault();
        if (user is null)
        {
            return _msg.UserNotFound<UserDetailDto>();
        }

        var roleNames =
            from ur in _db.UserRoles
            join r in _db.Roles on ur.RoleId equals r.Id
            where ur.UserId == request.Id && r.Name != null
            select r.Name!;
        var roles = await roleNames.ToListAsyncEither(cancellationToken).ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;
        var isActive = !user.LockoutEnabled || user.LockoutEnd is null || user.LockoutEnd < now;

        return _msg.Ok(new UserDetailDto(
            user.Id,
            user.Email,
            user.UserName,
            user.LocalePreference,
            user.KnowledgeLevel,
            user.Interests,
            user.CountryId,
            user.AvatarUrl,
            roles,
            isActive), "SUCCESS_OPERATION");
    }
}
