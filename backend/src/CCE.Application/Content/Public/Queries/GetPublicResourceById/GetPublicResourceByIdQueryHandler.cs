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
        return _messages.Ok(MapToDto(resource), "SUCCESS_OPERATION");
    }

    private PublicResourceDto MapToDto(Resource r)
    {
        var category = _db.ResourceCategories.FirstOrDefault(c => c.Id == r.CategoryId);
        var asset = _db.AssetFiles.FirstOrDefault(a => a.Id == r.AssetFileId);
        var countryIds = r.Countries.Select(c => c.CountryId).ToList();
        var countries = _db.Countries.Where(c => countryIds.Contains(c.Id)).ToList();

        return new PublicResourceDto(
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
            r.PublishedOn!.Value,
            r.ViewCount);
    }
}
