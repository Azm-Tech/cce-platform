using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Identity.Public.Dtos;
using CCE.Application.InterestManagement.Dtos;
using CCE.Application.Messages;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
        var users = await _db.Users
            .Where(u => u.Id == request.UserId && !u.IsDeleted)
            .Include(u => u.UserInterestTopics)
            .ThenInclude(uit => uit.InterestTopic)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var user = users.FirstOrDefault();
        if (user is null)
            return _msg.NotFound<UserProfileDto>(MessageKeys.Identity.USER_NOT_FOUND);

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
            user.AvatarUrl), MessageKeys.General.SUCCESS_OPERATION);
    }
}
