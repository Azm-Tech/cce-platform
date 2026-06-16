using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Content;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Content.Public.Queries.GetPublicResourceById;

public sealed class GetPublicResourceByIdQueryHandler : IRequestHandler<GetPublicResourceByIdQuery, Response<PublicResourceDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public GetPublicResourceByIdQueryHandler(ICceDbContext db, MessageFactory messages)
    {
        _db = db;
        _messages = messages;
    }

    public async Task<Response<PublicResourceDto>> Handle(GetPublicResourceByIdQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.Resources
            .AsNoTracking()
            .Include(r => r.Countries)
            .Where(r => r.Id == request.Id)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var resource = list.SingleOrDefault();
        if (resource is null || resource.PublishedOn is null)
            return _messages.ResourceNotFound<PublicResourceDto>();
        return _messages.Ok(await MapToDtoAsync(resource, cancellationToken).ConfigureAwait(false), "SUCCESS_OPERATION");
    }

    private async Task<PublicResourceDto> MapToDtoAsync(Resource r, CancellationToken ct)
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

        return new PublicResourceDto(
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
            publishedBy,
            r.PublishedOn!.Value,
            r.ViewCount,
            r.KnowledgeLevelId,
            r.JobSectorId);
    }

    private static string GetPublishedByName(string? firstName, string? lastName, string? userName)
    {
        var fullName = $"{firstName} {lastName}".Trim();
        return string.IsNullOrEmpty(fullName) ? userName ?? string.Empty : fullName;
    }
}
