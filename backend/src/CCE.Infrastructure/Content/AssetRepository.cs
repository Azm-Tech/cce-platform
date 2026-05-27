using CCE.Application.Content;
using CCE.Domain.Content;
using CCE.Infrastructure.Persistence;

namespace CCE.Infrastructure.Content;

public sealed class AssetRepository : Repository<AssetFile, System.Guid>, IAssetRepository
{
    public AssetRepository(CceDbContext db) : base(db) { }
}
