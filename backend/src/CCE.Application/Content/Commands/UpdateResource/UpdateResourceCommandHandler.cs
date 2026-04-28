using CCE.Application.Content;
using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Commands.UpdateResource;

public sealed class UpdateResourceCommandHandler : IRequestHandler<UpdateResourceCommand, ResourceDto?>
{
    private readonly IResourceService _service;

    public UpdateResourceCommandHandler(IResourceService service)
    {
        _service = service;
    }

    public async Task<ResourceDto?> Handle(UpdateResourceCommand request, CancellationToken cancellationToken)
    {
        var resource = await _service.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (resource is null)
        {
            return null;
        }

        resource.UpdateContent(
            request.TitleAr,
            request.TitleEn,
            request.DescriptionAr,
            request.DescriptionEn,
            request.ResourceType,
            request.CategoryId);

        await _service.UpdateAsync(resource, request.RowVersion, cancellationToken).ConfigureAwait(false);

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
