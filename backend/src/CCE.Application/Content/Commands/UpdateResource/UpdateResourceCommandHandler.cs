using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Content.Commands.UpdateResource;

public sealed class UpdateResourceCommandHandler : IRequestHandler<UpdateResourceCommand, Response<ResourceDto>>
{
    private readonly IRepository<Resource, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public UpdateResourceCommandHandler(
        IRepository<Resource, System.Guid> repo,
        ICceDbContext db,
        MessageFactory messages)
    {
        _repo = repo;
        _db = db;
        _messages = messages;
    }

    public async Task<Response<ResourceDto>> Handle(UpdateResourceCommand request, CancellationToken cancellationToken)
    {
        var resource = await _repo.GetByIdAsync(
            request.Id,
            q => q.Include(r => r.Countries),
            cancellationToken).ConfigureAwait(false);
        if (resource is null)
            return _messages.ResourceNotFound<ResourceDto>();

        var categoryExists = await ExistsAsync(_db.ResourceCategories.Where(c => c.Id == request.CategoryId), cancellationToken).ConfigureAwait(false);
        if (!categoryExists)
            return _messages.CategoryNotFound<ResourceDto>();

        var countryIds = request.CountryIds.Distinct().ToList();
        if (countryIds.Count > 0)
        {
            var existingCountryCount = await _db.Countries
                .Where(c => countryIds.Contains(c.Id))
                .CountAsyncEither(cancellationToken)
                .ConfigureAwait(false);
            if (existingCountryCount != countryIds.Count)
                return _messages.NotFound<ResourceDto>("COUNTRY_NOT_FOUND");
        }

        var expectedRowVersion = resource.RowVersion;
        resource.UpdateContent(
            request.TitleAr,
            request.TitleEn,
            request.DescriptionAr,
            request.DescriptionEn,
            request.ResourceType,
            request.CategoryId,
            request.CountryIds);

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

    private static async Task<bool> ExistsAsync<T>(IQueryable<T> query, CancellationToken ct)
    {
        var list = await query.Take(1).ToListAsyncEither(ct).ConfigureAwait(false);
        return list.Count > 0;
    }
}
