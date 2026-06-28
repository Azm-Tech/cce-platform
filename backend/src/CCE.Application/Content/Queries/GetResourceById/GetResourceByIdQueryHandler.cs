using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Content;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Content.Queries.GetResourceById;

public sealed class GetResourceByIdQueryHandler : IRequestHandler<GetResourceByIdQuery, Response<ResourceDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public GetResourceByIdQueryHandler(ICceDbContext db, MessageFactory messages)
    {
        _db = db;
        _messages = messages;
    }

    public async Task<Response<ResourceDto>> Handle(GetResourceByIdQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.Resources
            .AsNoTracking()
            .Include(r => r.Countries)
            .Where(r => r.Id == request.Id)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var resource = list.SingleOrDefault();
        return resource is null
            ? _messages.NotFound<ResourceDto>(MessageKeys.Content.RESOURCE_NOT_FOUND)
            : _messages.Ok(await MapToDtoAsync(resource, cancellationToken).ConfigureAwait(false), MessageKeys.General.SUCCESS_OPERATION);
    }

    private async Task<ResourceDto> MapToDtoAsync(Resource r, CancellationToken ct)
    {
        var countryIds = r.Countries.Select(c => c.CountryId).ToList();

        var categories = await _db.ResourceCategories
            .Where(c => c.Id == r.CategoryId)
            .Select(c => new { c.NameAr, c.NameEn })
            .ToListAsyncEither(ct)
            .ConfigureAwait(false);
        var category = categories.FirstOrDefault();

        var assets = await _db.AssetFiles
            .Where(a => a.Id == r.AssetFileId)
            .Select(a => new { a.OriginalFileName })
            .ToListAsyncEither(ct)
            .ConfigureAwait(false);
        var asset = assets.FirstOrDefault();

        var countries = await _db.Countries
            .Where(c => countryIds.Contains(c.Id))
            .Select(c => new { c.NameAr })
            .ToListAsyncEither(ct)
            .ConfigureAwait(false);

        var users = await _db.Users
            .Where(u => u.Id == r.UploadedById)
            .Select(u => new { u.FirstName, u.LastName, u.UserName })
            .ToListAsyncEither(ct)
            .ConfigureAwait(false);
        var user = users.FirstOrDefault();
        var publishedBy = GetPublishedByName(user?.FirstName, user?.LastName, user?.UserName);

        return new ResourceDto(
            r.Id,
            r.TitleAr,
            r.TitleEn,
            r.DescriptionAr,
            r.DescriptionEn,
            r.ResourceType,
            ResourceTypeAr.Get(r.ResourceType),
            r.CategoryId,
            category?.NameAr ?? string.Empty,
            category?.NameEn ?? string.Empty,
            r.AssetFileId,
            asset?.OriginalFileName ?? string.Empty,
            countryIds,
            countries.Select(c => c.NameAr).ToList(),
            r.UploadedById,
            publishedBy,
            r.PublishedOn,
            r.ViewCount,
            r.IsCenterManaged,
            r.IsPublished);
    }

    private static string GetPublishedByName(string? firstName, string? lastName, string? userName)
    {
        var fullName = $"{firstName} {lastName}".Trim();
        return string.IsNullOrEmpty(fullName) ? userName ?? string.Empty : fullName;
    }
}
