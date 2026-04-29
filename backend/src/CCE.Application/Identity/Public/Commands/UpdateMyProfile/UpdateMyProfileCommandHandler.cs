using CCE.Application.Identity.Public.Dtos;
using MediatR;

namespace CCE.Application.Identity.Public.Commands.UpdateMyProfile;

public sealed class UpdateMyProfileCommandHandler : IRequestHandler<UpdateMyProfileCommand, UserProfileDto?>
{
    private readonly IUserProfileService _service;

    public UpdateMyProfileCommandHandler(IUserProfileService service)
    {
        _service = service;
    }

    public async Task<UserProfileDto?> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _service.FindAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return null;
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
