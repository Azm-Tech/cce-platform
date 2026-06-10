using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Public.Dtos;
using CCE.Application.InterestManagement.Dtos;
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
        // fetch via repository
        var user = await _service.FindAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
            return _msg.UserNotFound<UserProfileDto>();

        // domain methods
        user.UpdateProfile(request.FirstName, request.LastName, request.JobTitle, request.OrganizationName);
        user.SetLocalePreference(request.LocalePreference);
        user.SetKnowledgeLevel(request.KnowledgeLevel);
        user.SetAvatarUrl(request.AvatarUrl);

        if (request.CountryId is null)
            user.ClearCountry();
        else
            user.AssignCountry(request.CountryId.Value);

        if (request.CountryCodeId is null)
            user.ClearCountryCode();
        else
            user.AssignCountryCode(request.CountryCodeId.Value);

        _service.Update(user);
        // ICceDbContext as unit of work
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var interestTopics = user.UserInterestTopics
            .Select(uit => new InterestTopicDto(
                uit.InterestTopic.Id,
                uit.InterestTopic.NameAr,
                uit.InterestTopic.NameEn,
                uit.InterestTopic.Category,
                uit.InterestTopic.IsActive))
            .ToList();

        return _msg.Ok(new UserProfileDto(
            user.Id,
            user.Email,
            user.UserName,
            user.FirstName,
            user.LastName,
            user.JobTitle,
            user.OrganizationName,
            user.PhoneNumber,
            user.LocalePreference,
            user.KnowledgeLevel,
            interestTopics,
            user.CountryId,
            user.CountryCodeId,
            user.AvatarUrl), "PROFILE_UPDATED");
    }
}
