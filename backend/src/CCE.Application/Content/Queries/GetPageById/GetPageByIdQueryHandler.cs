using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Queries.GetPageById;

public sealed class GetPageByIdQueryHandler : IRequestHandler<GetPageByIdQuery, PageDto?>
{
    private readonly ICceDbContext _db;

    public GetPageByIdQueryHandler(ICceDbContext db) => _db = db;

    public async Task<PageDto?> Handle(GetPageByIdQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.Pages.Where(p => p.Id == request.Id).ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var pageEntity = list.SingleOrDefault();
        return pageEntity is null ? null : MapToDto(pageEntity);
    }

    internal static PageDto MapToDto(Page p) => new(
        p.Id, p.Slug, p.PageType, p.TitleAr, p.TitleEn, p.ContentAr, p.ContentEn,
        System.Convert.ToBase64String(p.RowVersion));
}
