using System.IO;
using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Content;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Content.Queries.DownloadFile;

internal sealed class DownloadFileQueryHandler
    : IRequestHandler<DownloadFileQuery, Response<DownloadFilePayload>>
{
    private readonly ICceDbContext _db;
    private readonly IFileStorageFactory _storageFactory;
    private readonly MessageFactory _msg;

    public DownloadFileQueryHandler(
        ICceDbContext db,
        IFileStorageFactory storageFactory,
        MessageFactory msg)
    {
        _db = db;
        _storageFactory = storageFactory;
        _msg = msg;
    }

    public async Task<Response<DownloadFilePayload>> Handle(DownloadFileQuery request, CancellationToken ct)
    {
        if (request.Type == DownloadFileType.Media)
        {
            var media = await _db.MediaFiles
                .FirstOrDefaultAsync(m => m.Id == request.Id, ct)
                .ConfigureAwait(false);

            if (media is null)
                return _msg.MediaFileNotFound<DownloadFilePayload>();

            var storage = _storageFactory.GetStorage(DownloadFileType.Media);
            Stream stream;
            try
            {
                stream = await storage
                    .OpenReadAsync(media.StorageKey, ct)
                    .ConfigureAwait(false);
            }
            catch (FileNotFoundException)
            {
                return _msg.MediaFileNotFound<DownloadFilePayload>();
            }

            var payload = new DownloadFilePayload(stream, media.MimeType, media.OriginalFileName);
            return _msg.Ok(payload, "SUCCESS_OPERATION");
        }

        var asset = await _db.AssetFiles
            .FirstOrDefaultAsync(a => a.Id == request.Id, ct)
            .ConfigureAwait(false);

        if (asset is null)
            return _msg.AssetNotFound<DownloadFilePayload>();

        if (asset.VirusScanStatus != VirusScanStatus.Clean)
            return _msg.AssetNotClean<DownloadFilePayload>();

        var assetStorage = _storageFactory.GetStorage(DownloadFileType.Asset);
        Stream assetStream;
        try
        {
            assetStream = await assetStorage
                .OpenReadAsync(asset.Url, ct)
                .ConfigureAwait(false);
        }
        catch (FileNotFoundException)
        {
            return _msg.MediaFileNotFound<DownloadFilePayload>();
        }

        var assetPayload = new DownloadFilePayload(assetStream, asset.MimeType, asset.OriginalFileName);
        return _msg.Ok(assetPayload, "SUCCESS_OPERATION");
    }
}
