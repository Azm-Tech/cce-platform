using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Public.Dtos;
using CCE.Application.InterestManagement.Dtos;
using CCE.Application.Messages;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Identity.Public.Queries.GetMyInterests;

public sealed class GetMyInterestsQueryHandler
    : IRequestHandler<GetMyInterestsQuery, Response<UserInterestsDto>>
{
    private readonly IUserProfileRepository _service;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetMyInterestsQueryHandler(
        IUserProfileRepository service,
        ICceDbContext db,
        MessageFactory msg)
    {
        _service = service;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<UserInterestsDto>> Handle(
        GetMyInterestsQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _service.FindAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
            return _msg.UserNotFound<UserInterestsDto>();

        var currentTopics = await _db.InterestTopics
            .Where(t => t.IsActive)
            .ToListAsync(cancellationToken);

        var carbonAreaTopics = currentTopics
            .Where(t => t.Category == "carbon_area" && user.UserInterestTopics.Any(uit => uit.InterestTopicId == t.Id))
            .Select(t => new InterestTopicDto(t.Id, t.NameAr, t.NameEn, t.Category, t.IsActive))
            .ToList();

        var knowledgeAssessmentTopic = currentTopics
            .FirstOrDefault(t => t.Category == "knowledge_assessment" && user.UserInterestTopics.Any(uit => uit.InterestTopicId == t.Id));

        var jobSectorTopic = currentTopics
            .FirstOrDefault(t => t.Category == "job_sector" && user.UserInterestTopics.Any(uit => uit.InterestTopicId == t.Id));

        return _msg.Ok(new UserInterestsDto(
            carbonAreaTopics,
            knowledgeAssessmentTopic is not null
                ? new InterestTopicDto(knowledgeAssessmentTopic.Id, knowledgeAssessmentTopic.NameAr, knowledgeAssessmentTopic.NameEn, knowledgeAssessmentTopic.Category, knowledgeAssessmentTopic.IsActive)
                : null,
            jobSectorTopic is not null
                ? new InterestTopicDto(jobSectorTopic.Id, jobSectorTopic.NameAr, jobSectorTopic.NameEn, jobSectorTopic.Category, jobSectorTopic.IsActive)
                : null,
            user.CountryId), MessageKeys.General.SUCCESS_OPERATION);
    }
}