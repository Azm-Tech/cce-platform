using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Content.Commands.PublishResource;

public sealed class PublishResourceCommandHandler : IRequestHandler<PublishResourceCommand, Response<ResourceDto>>
{
    private readonly ICceDbContext _db;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _messages;

    public PublishResourceCommandHandler(
        ICceDbContext db,
        ISystemClock clock,
        MessageFactory messages)
    {
        _db = db;
        _clock = clock;
        _messages = messages;
    }

    public async Task<Response<ResourceDto>> Handle(PublishResourceCommand request, CancellationToken cancellationToken)
    {
        var resources = await _db.Resources
            .Include(r => r.Countries)
            .Where(r => r.Id == request.Id)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var resource = resources.SingleOrDefault();
        if (resource is null)
            return _messages.ResourceNotFound<ResourceDto>();

        var assets = await _db.AssetFiles
            .Where(a => a.Id == resource.AssetFileId)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var asset = assets.SingleOrDefault();
        if (asset is null)
            return _messages.AssetNotFound<ResourceDto>();
        if (asset.VirusScanStatus != VirusScanStatus.Clean)
            return _messages.AssetNotClean<ResourceDto>();

        var expectedRowVersion = resource.RowVersion;
        resource.Publish(_clock);

        _db.SetExpectedRowVersion(resource, expectedRowVersion);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var dto = MapToDto(resource);
        return _messages.Ok(dto, "SUCCESS_OPERATION");
    }

    private ResourceDto MapToDto(Resource r)
    {
        var category = _db.ResourceCategories.FirstOrDefault(c => c.Id == r.CategoryId);
        var asset = _db.AssetFiles.FirstOrDefault(a => a.Id == r.AssetFileId);
        var countryIds = r.Countries.Select(c => c.CountryId).ToList();
        var countries = _db.Countries.Where(c => countryIds.Contains(c.Id)).ToList();

        return new ResourceDto(
            r.Id,
            r.TitleAr,
            r.TitleEn,
            r.DescriptionAr,
            r.DescriptionEn,
            r.ResourceType,
            r.CategoryId,
            category?.NameAr ?? string.Empty,
            category?.NameEn ?? string.Empty,
            r.AssetFileId,
            asset?.OriginalFileName ?? string.Empty,
            countryIds,
            countries.Select(c => c.NameAr).ToList(),
            r.UploadedById,
            r.PublishedOn,
            r.ViewCount,
            r.IsCenterManaged,
            r.IsPublished);
    }
}
