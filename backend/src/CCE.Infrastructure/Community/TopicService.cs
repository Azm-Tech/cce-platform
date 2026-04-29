using CCE.Application.Community;
using CCE.Domain.Community;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Community;

public sealed class TopicService : ITopicService
{
    private readonly CceDbContext _db;

    public TopicService(CceDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(Topic topic, CancellationToken ct)
    {
        _db.Topics.Add(topic);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<Topic?> FindAsync(System.Guid id, CancellationToken ct)
    {
        return await _db.Topics.FirstOrDefaultAsync(t => t.Id == id, ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Topic topic, CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
