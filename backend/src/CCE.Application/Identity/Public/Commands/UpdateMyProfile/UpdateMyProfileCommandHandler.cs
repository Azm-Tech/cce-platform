using CCE.Application.Common;
using CCE.Application.Identity.Public.Dtos;
using MediatR;

namespace CCE.Application.Identity.Public.Commands.UpdateMyProfile;

public sealed class UpdateMyProfileCommandHandler : IRequestHandler<UpdateMyProfileCommand, Result<UserProfileDto>>
{
    private readonly IUserProfileRepository _service;
    private readonly CCE.Application.Common.Errors _errors;

    public UpdateMyProfileCommandHandler(IUserProfileRepository service, CCE.Application.Common.Errors errors)
    {
        _service = service;
        _errors = errors;
    }

    public async Task<Result<UserProfileDto>> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _service.FindAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return _errors.UserNotFound();
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

        await _service.UpdateAsync(user, cancellationToken).ConfigureAwait(false);

        return new UserProfileDto(
            user.Id,
            user.Email,
            user.UserName,
            user.LocalePreference,
            user.KnowledgeLevel,
            user.Interests,
            user.CountryId,
            user.AvatarUrl);
    }
}
