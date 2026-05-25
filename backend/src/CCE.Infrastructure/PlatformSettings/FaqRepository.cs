using CCE.Application.PlatformSettings;
using CCE.Domain.PlatformSettings;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.PlatformSettings;

public sealed class FaqRepository : IFaqRepository
{
    private readonly CceDbContext _db;

    public FaqRepository(CceDbContext db) => _db = db;

    public async Task<Faq?> GetByIdAsync(System.Guid id, CancellationToken ct)
        => await _db.Faqs
            .FirstOrDefaultAsync(f => f.Id == id, ct)
            .ConfigureAwait(false);

    public void Add(Faq faq) => _db.Faqs.Add(faq);

    public void Delete(Faq faq) => _db.Faqs.Remove(faq);
}
