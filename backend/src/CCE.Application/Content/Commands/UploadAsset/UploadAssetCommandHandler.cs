using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Dtos;
using CCE.Domain.Common;
using CCE.Domain.Content;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CCE.Application.Content.Commands.UploadAsset;

public sealed class UploadAssetCommandHandler : IRequestHandler<UploadAssetCommand, AssetFileDto>
{
    private readonly IFileStorage _storage;
    private readonly IClamAvScanner _scanner;
    private readonly IAssetService _service;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;
    private readonly ILogger<UploadAssetCommandHandler> _logger;

    public UploadAssetCommandHandler(
        IFileStorage storage,
        IClamAvScanner scanner,
        IAssetService service,
        ICurrentUserAccessor currentUser,
        ISystemClock clock,
        ILogger<UploadAssetCommandHandler> logger)
    {
        _storage = storage;
        _scanner = scanner;
        _service = service;
        _currentUser = currentUser;
        _clock = clock;
        _logger = logger;
    }

    public async Task<AssetFileDto> Handle(UploadAssetCommand request, CancellationToken cancellationToken)
    {
        var uploadedById = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot upload an asset from a request without a user identity.");

        // Buffer the content once so we can save AND scan without re-reading the request body.
        await using var buffer = new MemoryStream();
        await request.Content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        buffer.Position = 0;

        var storageKey = await _storage.SaveAsync(buffer, request.OriginalFileName, cancellationToken).ConfigureAwait(false);

        buffer.Position = 0;
        var scanResult = await _scanner.ScanAsync(buffer, cancellationToken).ConfigureAwait(false);

        var asset = AssetFile.Register(
            url: storageKey,
            originalFileName: request.OriginalFileName,
            sizeBytes: request.SizeBytes,
            mimeType: request.MimeType,
            uploadedById: uploadedById,
            clock: _clock);

        switch (scanResult)
        {
            case VirusScanResult.Clean:
                asset.MarkClean(_clock);
                break;
            case VirusScanResult.Infected:
                asset.MarkInfected(_clock);
                await _storage.DeleteAsync(storageKey, cancellationToken).ConfigureAwait(false);
                _logger.LogWarning("Infected asset {AssetId} ({FileName}) — storage object purged.", asset.Id, request.OriginalFileName);
                break;
            case VirusScanResult.ScanFailed:
                asset.MarkScanFailed(_clock);
                _logger.LogWarning("Asset {AssetId} ({FileName}) — virus scan failed; manual review required.", asset.Id, request.OriginalFileName);
                break;
        }

        await _service.SaveAsync(asset, cancellationToken).ConfigureAwait(false);

        return new AssetFileDto(
            asset.Id,
            asset.Url,
            asset.OriginalFileName,
            asset.SizeBytes,
            asset.MimeType,
            asset.UploadedById,
            asset.UploadedOn,
            asset.VirusScanStatus,
            asset.ScannedOn);
    }
}
