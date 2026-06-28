using CCE.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Content;

public sealed class UserContentInterestResolver : IUserContentInterestResolver
{
    private const string KnowledgeAssessmentCategory = "knowledge_assessment";
    private const string JobSectorCategory = "job_sector";

    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public UserContentInterestResolver(ICceDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<(System.Guid? KnowledgeLevelId, System.Guid? JobSectorId)> ResolveAsync(
        System.Guid? explicitKnowledgeLevelId,
        System.Guid? explicitJobSectorId,
        CancellationToken ct)
    {
        if (explicitKnowledgeLevelId.HasValue && explicitJobSectorId.HasValue)
            return (explicitKnowledgeLevelId, explicitJobSectorId);

        var userId = _currentUser.GetUserId();
        if (!userId.HasValue)
            return (explicitKnowledgeLevelId, explicitJobSectorId);

        var user = await _db.Users
            .Where(u => u.Id == userId.Value)
            .Select(u => new
            {
                KaId = u.UserInterestTopics
                    .Select(uit => uit.InterestTopic)
                    .Where(it => it.Category == KnowledgeAssessmentCategory)
                    .Select(it => (System.Guid?)it.Id)
                    .FirstOrDefault(),
                JsId = u.UserInterestTopics
                    .Select(uit => uit.InterestTopic)
                    .Where(it => it.Category == JobSectorCategory)
                    .Select(it => (System.Guid?)it.Id)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (user is null)
            return (explicitKnowledgeLevelId, explicitJobSectorId);

        return (explicitKnowledgeLevelId ?? user.KaId, explicitJobSectorId ?? user.JsId);
    }
}
