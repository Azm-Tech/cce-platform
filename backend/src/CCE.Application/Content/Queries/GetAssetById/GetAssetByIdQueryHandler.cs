using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Messages;
using MediatR;

namespace CCE.Application.Content.Queries.GetAssetById;

public sealed class GetAssetByIdQueryHandler : IRequestHandler<GetAssetByIdQuery, Response<AssetFileDto>>
{
    private readonly ICceDbContext _db;
    private readonly IFileStorage _storage;
    private readonly MessageFactory _msg;

    public GetAssetByIdQueryHandler(ICceDbContext db, IFileStorage storage, MessageFactory msg)
    {
        _db = db;
        _storage = storage;
        _msg = msg;
    }

    public async Task<Response<AssetFileDto>> Handle(GetAssetByIdQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.AssetFiles
            .Where(a => a.Id == request.Id)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var asset = list.SingleOrDefault();
        if (asset is null)
            return _msg.NotFound<AssetFileDto>(MessageKeys.Content.ASSET_NOT_FOUND);

        var publicUrl = _storage.GetPublicUrl(asset.Url).ToString();

        return _msg.Ok(new AssetFileDto(
            asset.Id,
            publicUrl,
            asset.OriginalFileName,
            asset.SizeBytes,
            asset.MimeType,
            asset.UploadedById,
            asset.UploadedOn,
            asset.VirusScanStatus,
            asset.ScannedOn), MessageKeys.General.SUCCESS_OPERATION);
    }
}
