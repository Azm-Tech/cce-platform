using CCE.Application.Content;
using Microsoft.Extensions.DependencyInjection;

namespace CCE.Infrastructure.Files;

internal sealed class FileStorageFactory : IFileStorageFactory
{
    private readonly IFileStorage _assetStorage;
    private readonly IFileStorage _mediaStorage;

    public FileStorageFactory(
        IFileStorage assetStorage,
        [FromKeyedServices("media")] IFileStorage mediaStorage)
    {
        _assetStorage = assetStorage;
        _mediaStorage = mediaStorage;
    }

    public IFileStorage GetStorage(DownloadFileType fileType) => fileType switch
    {
        DownloadFileType.Media => _mediaStorage,
        _ => _assetStorage,
    };
}
