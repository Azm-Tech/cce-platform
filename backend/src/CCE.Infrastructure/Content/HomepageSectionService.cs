using CCE.Application.Content;
using CCE.Domain.Content;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Content;

public sealed class HomepageSectionService : IHomepageSectionService
{
    private readonly CceDbContext _db;

    public HomepageSectionService(CceDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(HomepageSection section, CancellationToken ct)
    {
        _db.HomepageSections.Add(section);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<HomepageSection?> FindAsync(System.Guid id, CancellationToken ct)
    {
        return await _db.HomepageSections.FirstOrDefaultAsync(s => s.Id == id, ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(HomepageSection section, CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task ReorderAsync(
        System.Collections.Generic.IReadOnlyList<(System.Guid Id, int OrderIndex)> assignments,
        CancellationToken ct)
    {
        var ids = assignments.Select(a => a.Id).ToList();
        var sections = await _db.HomepageSections
            .Where(s => ids.Contains(s.Id))
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var byId = sections.ToDictionary(s => s.Id);
        foreach (var (id, orderIndex) in assignments)
        {
            if (byId.TryGetValue(id, out var section))
            {
                section.Reorder(orderIndex);
            }
        }
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
