using CCE.Application.Common.Interfaces;
using CCE.Domain.Content;

namespace CCE.Application.Content;

public interface IAssetRepository : IRepository<AssetFile, System.Guid>
{
}
