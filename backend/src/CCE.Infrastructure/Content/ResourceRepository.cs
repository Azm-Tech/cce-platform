using CCE.Application.Content;
using CCE.Domain.Content;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Content;

public sealed class ResourceRepository : IResourceRepository
{
    private readonly CceDbContext _db;

    public ResourceRepository(CceDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(Resource resource, CancellationToken ct)
    {
        _db.Resources.Add(resource);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<Resource?> FindAsync(System.Guid id, CancellationToken ct)
    {
        return await _db.Resources.FirstOrDefaultAsync(r => r.Id == id, ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Resource resource, byte[] expectedRowVersion, CancellationToken ct)
    {
        _db.SetExpectedRowVersion(resource, expectedRowVersion);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
