using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.InterestManagement.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Identity;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Identity.Public.Commands.UserInterest;

public sealed class UpsertUserInterestCommandHandler
    : IRequestHandler<UpsertUserInterestCommand, Response<UpsertUserInterestResult>>
{
    private readonly IUserProfileRepository _service;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public UpsertUserInterestCommandHandler(
        IUserProfileRepository service,
        ICceDbContext db,
        MessageFactory msg)
    {
        _service = service;
        _db = db;
        _msg = msg;
    }

    public async Task<Response<UpsertUserInterestResult>> Handle(
        UpsertUserInterestCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _service.FindAsync(request.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null)
            return _msg.UserNotFound<UpsertUserInterestResult>();

        var errors = new List<FieldError>();

        // Validate interest topic IDs exist with correct category
        var validTopics = await _db.InterestTopics
            .Where(t => t.IsActive)
            .Select(t => new { t.Id, t.Category })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var validByCategory = validTopics
            .GroupBy(t => t.Category)
            .ToDictionary(g => g.Key, g => g.Select(t => t.Id).ToHashSet());

        if (request.CarbonAreaIds?.Count > 0)
        {
            var validCarbon = validByCategory.GetValueOrDefault("carbon_area") ?? [];
            var invalid = request.CarbonAreaIds.Where(id => !validCarbon.Contains(id)).ToList();
            if (invalid.Count > 0)
                errors.Add(_msg.Field("carbonAreaIds", "INTEREST_TOPIC_NOT_FOUND"));
        }

        if (request.KnowledgeAssessmentId.HasValue)
        {
            var validKa = validByCategory.GetValueOrDefault("knowledge_assessment") ?? [];
            if (!validKa.Contains(request.KnowledgeAssessmentId.Value))
                errors.Add(_msg.Field("knowledgeAssessmentId", "INTEREST_TOPIC_NOT_FOUND"));
        }

        if (request.JobSectorId.HasValue)
        {
            var validJs = validByCategory.GetValueOrDefault("job_sector") ?? [];
            if (!validJs.Contains(request.JobSectorId.Value))
                errors.Add(_msg.Field("jobSectorId", "INTEREST_TOPIC_NOT_FOUND"));
        }

        if (request.TargetCountryId.HasValue)
        {
            var countryExists = await _db.Countries
                .AnyAsync(c => c.Id == request.TargetCountryId.Value, cancellationToken)
                .ConfigureAwait(false);
            if (!countryExists)
                errors.Add(_msg.Field("targetCountryId", "COUNTRY_NOT_FOUND"));
        }

        if (errors.Count > 0)
            return _msg.ValidationError<UpsertUserInterestResult>("VALIDATION_ERROR", errors);

        // Load category mapping for all interest topics (for filtering by category)
        var topicCategoryMap = validTopics
            .ToDictionary(t => t.Id, t => t.Category);

        // carbon_area — multiple select
        UpsertCategory(user, request.CarbonAreaIds, "carbon_area", topicCategoryMap);

        // knowledge_assessment — single select
        UpsertCategory(user, request.KnowledgeAssessmentId is not null ? [request.KnowledgeAssessmentId.Value] : null, "knowledge_assessment", topicCategoryMap);

        // job_sector — single select
        UpsertCategory(user, request.JobSectorId is not null ? [request.JobSectorId.Value] : null, "job_sector", topicCategoryMap);

        // target country — single select
        if (request.TargetCountryId.HasValue)
            user.AssignCountry(request.TargetCountryId.Value);
        else
            user.ClearCountry();

        _service.Update(user);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

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

        return _msg.InterestUpserted(new UpsertUserInterestResult(
            carbonAreaTopics,
            knowledgeAssessmentTopic is not null ? new InterestTopicDto(knowledgeAssessmentTopic.Id, knowledgeAssessmentTopic.NameAr, knowledgeAssessmentTopic.NameEn, knowledgeAssessmentTopic.Category, knowledgeAssessmentTopic.IsActive) : null,
            jobSectorTopic is not null ? new InterestTopicDto(jobSectorTopic.Id, jobSectorTopic.NameAr, jobSectorTopic.NameEn, jobSectorTopic.Category, jobSectorTopic.IsActive) : null,
            user.CountryId));
    }

    private static void UpsertCategory(
        User user,
        IReadOnlyList<System.Guid>? newIds,
        string category,
        Dictionary<System.Guid, string> topicCategoryMap)
    {
        var newSet = newIds?.Distinct().ToHashSet() ?? [];

        var toRemove = user.UserInterestTopics
            .Where(uit =>
            {
                var cat = topicCategoryMap.GetValueOrDefault(uit.InterestTopicId);
                return cat == category && !newSet.Contains(uit.InterestTopicId);
            })
            .ToList();

        var existingInCategory = user.UserInterestTopics
            .Where(uit =>
            {
                var cat = topicCategoryMap.GetValueOrDefault(uit.InterestTopicId);
                return cat == category;
            })
            .Select(uit => uit.InterestTopicId)
            .ToHashSet();

        var toAddIds = newSet
            .Where(id => !existingInCategory.Contains(id))
            .ToList();

        foreach (var remove in toRemove)
            user.UserInterestTopics.Remove(remove);
        foreach (var id in toAddIds)
            user.UserInterestTopics.Add(new UserInterestTopic
            {
                UserId = user.Id,
                InterestTopicId = id
            });
    }
}