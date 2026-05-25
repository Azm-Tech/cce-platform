using CCE.Application.Community;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Community;

public sealed class CommunityReadService : ICommunityReadService
{
    private readonly CceDbContext _db;

    public CommunityReadService(CceDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<System.Guid>> GetTopicFollowerIdsAsync(
        System.Guid topicId,
        System.Guid? excludeUserId,
        CancellationToken ct)
    {
        var query = _db.TopicFollows.Where(f => f.TopicId == topicId);

        if (excludeUserId is { } excl)
        {
            query = query.Where(f => f.UserId != excl);
        }

        var ids = await query
            .Select(f => f.UserId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return ids;
    }
}
