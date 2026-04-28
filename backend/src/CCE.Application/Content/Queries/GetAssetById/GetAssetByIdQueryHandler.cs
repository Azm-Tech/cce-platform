using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Queries.GetAssetById;

public sealed class GetAssetByIdQueryHandler : IRequestHandler<GetAssetByIdQuery, AssetFileDto?>
{
    private readonly IAssetService _service;

    public GetAssetByIdQueryHandler(IAssetService service)
    {
        _service = service;
    }

    public async Task<AssetFileDto?> Handle(GetAssetByIdQuery request, CancellationToken cancellationToken)
    {
        var asset = await _service.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
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
