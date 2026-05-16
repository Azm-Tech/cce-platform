using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Public.Dtos;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Identity.Public.Commands.UpdateMyProfile;

public sealed class UpdateMyProfileCommandHandler : IRequestHandler<UpdateMyProfileCommand, Response<UserProfileDto>>
{
    private readonly ICceDbContext _db;
    private readonly IUserProfileRepository _service;
    private readonly MessageFactory _msg;

    public UpdateMyProfileCommandHandler(ICceDbContext db, IUserProfileRepository service, MessageFactory msg)
    {
        _db = db;
        _service = service;
        _msg = msg;
    }

    public async Task<Response<UserProfileDto>> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _service.FindAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return _msg.UserNotFound<UserProfileDto>();
        }

        user.SetLocalePreference(request.LocalePreference);
        user.SetKnowledgeLevel(request.KnowledgeLevel);
        user.UpdateInterests(request.Interests);
        user.SetAvatarUrl(request.AvatarUrl);

        if (request.CountryId is null)
        {
            user.ClearCountry();
        }
        else
        {
            user.AssignCountry(request.CountryId.Value);
        }

        _service.Update(user);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _msg.Ok(new UserProfileDto(
            user.Id,
            user.Email,
            user.UserName,
            user.LocalePreference,
            user.KnowledgeLevel,
            user.Interests,
            user.CountryId,
            user.AvatarUrl), "PROFILE_UPDATED");
    }
}
