using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Public.Dtos;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Identity.Public.Queries.GetMyProfile;

public sealed class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, Response<UserProfileDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetMyProfileQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<UserProfileDto>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var rows = await _db.Users
            .Where(u => u.Id == request.UserId && !u.IsDeleted)
            .Select(u => new UserProfileDto(
                u.Id,
                u.Email,
                u.UserName,
                u.FirstName,
                u.LastName,
                u.JobTitle,
                u.OrganizationName,
                u.PhoneNumber,
                u.LocalePreference,
                u.KnowledgeLevel,
                u.Interests,
                u.CountryId,
                u.CountryCodeId,
                u.AvatarUrl))
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var dto = rows.FirstOrDefault();
        if (dto is null)
            return _msg.UserNotFound<UserProfileDto>();

        return _msg.Ok(dto, "SUCCESS_OPERATION");
    }
}
