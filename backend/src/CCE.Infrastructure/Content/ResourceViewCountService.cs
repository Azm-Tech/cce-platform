using CCE.Application.Content.Public;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Content;

public sealed class ResourceViewCountService : IResourceViewCountService
{
    private readonly CceDbContext _db;

    public ResourceViewCountService(CceDbContext db)
    {
        _db = db;
    }

    public async Task IncrementAsync(System.Guid resourceId, CancellationToken ct)
    {
        // EF Core 7+ bulk update — atomic single-statement, no entity load.
        await _db.Resources
            .Where(r => r.Id == resourceId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.ViewCount, r => r.ViewCount + 1), ct)
            .ConfigureAwait(false);
    }
}
