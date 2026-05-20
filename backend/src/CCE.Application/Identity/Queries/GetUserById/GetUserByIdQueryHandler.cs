using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Dtos;
using CCE.Application.Messages;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

    public async Task<Response<UserDetailDto>> Handle(
    GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _db.Users
            .Where(u => u.Id == request.Id && !u.IsDeleted)
            .Select(u => new UserDetailDto(
                u.Id,
                u.Email,
                u.UserName,
                u.LocalePreference,
                u.KnowledgeLevel,
                u.Interests,
                u.CountryId,
                u.AvatarUrl,
                _db.UserRoles
                    .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                    .Where(x => x.UserId == u.Id && x.Name != null)
                    .Select(x => x.Name!)
                    .ToList(),
                u.Status == Domain.Identity.UserStatus.Active))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return dto is null
            ? _msg.UserNotFound<UserDetailDto>()
            : _msg.Ok(dto, "SUCCESS_OPERATION");
    }
}
