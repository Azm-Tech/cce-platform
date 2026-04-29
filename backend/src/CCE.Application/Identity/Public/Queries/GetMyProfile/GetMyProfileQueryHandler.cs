using CCE.Application.Identity.Public.Dtos;
using MediatR;

namespace CCE.Application.Identity.Public.Queries.GetMyProfile;

public sealed class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, UserProfileDto?>
{
    private readonly IUserProfileService _service;

    public GetMyProfileQueryHandler(IUserProfileService service)
    {
        _service = service;
    }

    public async Task<UserProfileDto?> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _service.FindAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return null;
        }

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
