using CCE.Application.Common;
using CCE.Application.Identity.Public.Dtos;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Identity.Public.Queries.GetMyProfile;

public sealed class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, Response<UserProfileDto>>
{
    private readonly IUserProfileRepository _service;
    private readonly MessageFactory _msg;

    public GetMyProfileQueryHandler(IUserProfileRepository service, MessageFactory msg)
    {
        _service = service;
        _msg = msg;
    }

    public async Task<Response<UserProfileDto>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _service.FindAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return _msg.UserNotFound<UserProfileDto>();
        }

        return _msg.Ok(new UserProfileDto(
            user.Id,
            user.Email,
            user.UserName,
            user.LocalePreference,
            user.KnowledgeLevel,
            user.Interests,
            user.CountryId,
            user.AvatarUrl), "SUCCESS_OPERATION");
    }
}
