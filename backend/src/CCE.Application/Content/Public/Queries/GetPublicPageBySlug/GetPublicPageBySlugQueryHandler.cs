using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Public.Queries.GetPublicPageBySlug;

public sealed class GetPublicPageBySlugQueryHandler : IRequestHandler<GetPublicPageBySlugQuery, PublicPageDto?>
{
    private readonly ICceDbContext _db;

    public GetPublicPageBySlugQueryHandler(ICceDbContext db) => _db = db;

    public async Task<PublicPageDto?> Handle(GetPublicPageBySlugQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.Pages
            .Where(p => p.Slug == request.Slug)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var pageEntity = list.SingleOrDefault();
        return pageEntity is null ? null : MapToDto(pageEntity);
    }

    internal static PublicPageDto MapToDto(Page p) => new(
        p.Id,
        p.Slug,
        p.PageType,
        p.TitleAr,
        p.TitleEn,
        p.ContentAr,
        p.ContentEn);
}
