using CCE.Application.PlatformSettings;
using CCE.Domain.PlatformSettings;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.PlatformSettings;

public sealed class KnowledgePartnerRepository : IKnowledgePartnerRepository
{
    private readonly CceDbContext _db;

    public KnowledgePartnerRepository(CceDbContext db) => _db = db;

    public async Task<KnowledgePartner?> FindAsync(System.Guid id, CancellationToken ct)
        => await _db.KnowledgePartners.FirstOrDefaultAsync(p => p.Id == id, ct).ConfigureAwait(false);
}
