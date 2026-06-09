using System.Linq;
using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Dtos;
using CCE.Application.InterestManagement.Dtos;
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

    public async Task<Response<UserDetailDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var users = await _db.Users
            .Where(u => u.Id == request.Id)
            .Include(u => u.UserInterestTopics)
            .ThenInclude(uit => uit.InterestTopic)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var user = users.SingleOrDefault();
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

        var interestTopics = user.UserInterestTopics
            .Select(uit => new InterestTopicDto(
                uit.InterestTopic.Id,
                uit.InterestTopic.NameAr,
                uit.InterestTopic.NameEn,
                uit.InterestTopic.Category,
                uit.InterestTopic.IsActive))
            .ToList();

        return _msg.Ok(new UserDetailDto(
            user.Id,
            user.Email,
            user.UserName,
            user.LocalePreference,
            user.KnowledgeLevel,
            interestTopics,
            user.CountryId,
            user.AvatarUrl,
            roles,
            isActive), "SUCCESS_OPERATION");
    }
}
