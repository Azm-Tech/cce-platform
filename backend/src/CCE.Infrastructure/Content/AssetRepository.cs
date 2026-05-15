using CCE.Application.Content;
using CCE.Domain.Content;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Content;

public sealed class AssetRepository : IAssetRepository
{
    private readonly CceDbContext _db;

    public AssetRepository(CceDbContext db)
    {
        _db = db;
    }

    public async Task SaveAsync(AssetFile asset, CancellationToken ct)
    {
        _db.AssetFiles.Add(asset);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<AssetFile?> FindAsync(System.Guid id, CancellationToken ct)
    {
        return await _db.AssetFiles.FindAsync(new object[] { id }, ct).ConfigureAwait(false);
    }
}
