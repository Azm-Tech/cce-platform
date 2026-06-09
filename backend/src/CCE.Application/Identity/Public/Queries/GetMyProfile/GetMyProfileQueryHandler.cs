using CCE.Application.Common;
using CCE.Application.Identity.Public.Dtos;
using CCE.Application.InterestManagement.Dtos;
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
            user.LocalePreference,
            user.KnowledgeLevel,
            interestTopics,
            user.CountryId,
            user.AvatarUrl), "SUCCESS_OPERATION");
    }
}
