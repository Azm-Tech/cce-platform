using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Queries.GetAssetById;

public sealed class GetAssetByIdQueryHandler : IRequestHandler<GetAssetByIdQuery, AssetFileDto?>
{
    private readonly ICceDbContext _db;

    public GetAssetByIdQueryHandler(ICceDbContext db) => _db = db;

    public async Task<AssetFileDto?> Handle(GetAssetByIdQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.AssetFiles
            .Where(a => a.Id == request.Id)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var asset = list.SingleOrDefault();
        if (asset is null)
        {
            return null;
        }
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
