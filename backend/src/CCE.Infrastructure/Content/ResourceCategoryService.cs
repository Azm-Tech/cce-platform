using CCE.Application.Content;
using CCE.Domain.Content;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Content;

public sealed class ResourceCategoryService : IResourceCategoryService
{
    private readonly CceDbContext _db;

    public ResourceCategoryService(CceDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(ResourceCategory category, CancellationToken ct)
    {
        _db.ResourceCategories.Add(category);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<ResourceCategory?> FindAsync(System.Guid id, CancellationToken ct)
    {
        return await _db.ResourceCategories.FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(ResourceCategory category, CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
