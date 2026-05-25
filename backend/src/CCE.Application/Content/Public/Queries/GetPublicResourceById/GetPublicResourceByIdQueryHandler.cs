using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Public.Queries.GetPublicResourceById;

public sealed class GetPublicResourceByIdQueryHandler : IRequestHandler<GetPublicResourceByIdQuery, PublicResourceDto?>
{
    private readonly ICceDbContext _db;

    public GetPublicResourceByIdQueryHandler(ICceDbContext db) => _db = db;

    public async Task<PublicResourceDto?> Handle(GetPublicResourceByIdQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.Resources
            .Where(r => r.Id == request.Id)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var resource = list.SingleOrDefault();
        if (resource is null || resource.PublishedOn is null)
        {
            return null;
        }
        return MapToDto(resource);
    }

    internal static PublicResourceDto MapToDto(Resource r) => new(
        r.Id,
        r.TitleAr,
        r.TitleEn,
        r.DescriptionAr,
        r.DescriptionEn,
        r.ResourceType,
        r.CategoryId,
        r.CountryId,
        r.AssetFileId,
        r.PublishedOn!.Value,
        r.ViewCount);
}
