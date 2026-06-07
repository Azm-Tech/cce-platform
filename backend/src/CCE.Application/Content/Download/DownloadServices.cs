using CCE.Application.Common.Interfaces;
using CCE.Domain.Content;
using CCE.Domain.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CCE.Application.Content.Download;

public sealed record DownloadResult(Stream Stream, string MimeType, string FileName);

public interface IDownloadService
{
    Task<DownloadResult?> DownloadAsync(Guid id, CancellationToken ct);
}

public sealed class AssetDownloadService(ICceDbContext db, IFileStorage storage)
    : IDownloadService
{
    public async Task<DownloadResult?> DownloadAsync(Guid id, CancellationToken ct)
    {
        var asset = await db.AssetFiles.FirstOrDefaultAsync(a => a.Id == id, ct).ConfigureAwait(false);
        if (asset is null || asset.VirusScanStatus != VirusScanStatus.Clean)
            return null;

        var stream = await storage.OpenReadAsync(asset.Url, ct).ConfigureAwait(false);
        return new DownloadResult(stream, asset.MimeType, asset.OriginalFileName);
    }
}

public sealed class MediaDownloadService(ICceDbContext db, [FromKeyedServices("media")] IFileStorage storage)
    : IDownloadService
{
    public async Task<DownloadResult?> DownloadAsync(Guid id, CancellationToken ct)
    {
        var media = await db.MediaFiles.FirstOrDefaultAsync(m => m.Id == id, ct).ConfigureAwait(false);
        if (media is null)
            return null;

        var stream = await storage.OpenReadAsync(media.StorageKey, ct).ConfigureAwait(false);
        return new DownloadResult(stream, media.MimeType, media.OriginalFileName);
    }
}

public sealed class DownloadServiceFactory(IServiceProvider sp)
{
    public IDownloadService Create(DownloadType type) => type switch
    {
        DownloadType.Asset => sp.GetRequiredService<AssetDownloadService>(),
        DownloadType.Image => sp.GetRequiredService<MediaDownloadService>(),
        _ => throw new NotSupportedException($"Download type '{type}' is not supported.")
    };
}
