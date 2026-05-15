using CCE.Application.Common.Interfaces;
using CCE.Application.Content;
using CCE.Application.Content.Dtos;
using CCE.Domain.Common;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.CreateResource;

public sealed class CreateResourceCommandHandler : IRequestHandler<CreateResourceCommand, ResourceDto>
{
    private readonly IResourceRepository _service;
    private readonly IAssetRepository _assetService;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ISystemClock _clock;

    public CreateResourceCommandHandler(
        IResourceRepository service,
        IAssetRepository assetService,
        ICurrentUserAccessor currentUser,
        ISystemClock clock)
    {
        _service = service;
        _assetService = assetService;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<ResourceDto> Handle(CreateResourceCommand request, CancellationToken cancellationToken)
    {
        var asset = await _assetService.FindAsync(request.AssetFileId, cancellationToken).ConfigureAwait(false);
        if (asset is null)
        {
            throw new System.Collections.Generic.KeyNotFoundException($"Asset {request.AssetFileId} not found.");
        }
        if (asset.VirusScanStatus != VirusScanStatus.Clean)
        {
            throw new DomainException($"Asset {request.AssetFileId} has not passed virus scan ({asset.VirusScanStatus}).");
        }

        var uploadedById = _currentUser.GetUserId()
            ?? throw new DomainException("Cannot create a resource from a request without a user identity.");

        var resource = Resource.Draft(
            request.TitleAr,
            request.TitleEn,
            request.DescriptionAr,
            request.DescriptionEn,
            request.ResourceType,
            request.CategoryId,
            request.CountryId,
            uploadedById,
            request.AssetFileId,
            _clock);

        await _service.SaveAsync(resource, cancellationToken).ConfigureAwait(false);

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
