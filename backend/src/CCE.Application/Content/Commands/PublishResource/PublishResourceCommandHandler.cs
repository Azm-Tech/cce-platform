using CCE.Application.Common.Interfaces;
using CCE.Application.Content;
using CCE.Application.Content.Dtos;
using CCE.Domain.Common;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.PublishResource;

public sealed class PublishResourceCommandHandler : IRequestHandler<PublishResourceCommand, ResourceDto?>
{
    private readonly IResourceService _service;
    private readonly IAssetService _assetService;
    private readonly ISystemClock _clock;

    public PublishResourceCommandHandler(
        IResourceService service,
        IAssetService assetService,
        ISystemClock clock)
    {
        _service = service;
        _assetService = assetService;
        _clock = clock;
    }

    public async Task<ResourceDto?> Handle(PublishResourceCommand request, CancellationToken cancellationToken)
    {
        var resource = await _service.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (resource is null)
        {
            return null;
        }

        var asset = await _assetService.FindAsync(resource.AssetFileId, cancellationToken).ConfigureAwait(false);
        if (asset is null)
        {
            throw new DomainException($"Asset {resource.AssetFileId} not found for resource {resource.Id}.");
        }
        if (asset.VirusScanStatus != VirusScanStatus.Clean)
        {
            throw new DomainException($"Cannot publish resource {resource.Id}: asset has not passed virus scan ({asset.VirusScanStatus}).");
        }

        var expectedRowVersion = resource.RowVersion;
        resource.Publish(_clock);
        await _service.UpdateAsync(resource, expectedRowVersion, cancellationToken).ConfigureAwait(false);

        return new ResourceDto(
            resource.Id,
            resource.TitleAr,
            resource.TitleEn,
            resource.DescriptionAr,
            resource.DescriptionEn,
            resource.ResourceType,
            resource.CategoryId,
            resource.CountryId,
            resource.UploadedById,
            resource.AssetFileId,
            resource.PublishedOn,
            resource.ViewCount,
            resource.IsCenterManaged,
            resource.IsPublished,
            System.Convert.ToBase64String(resource.RowVersion));
    }
}
