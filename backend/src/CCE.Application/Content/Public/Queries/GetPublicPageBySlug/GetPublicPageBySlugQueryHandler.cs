using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Public.Queries.GetPublicPageBySlug;

public sealed class GetPublicPageBySlugQueryHandler : IRequestHandler<GetPublicPageBySlugQuery, Response<PublicPageDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetPublicPageBySlugQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PublicPageDto>> Handle(GetPublicPageBySlugQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.Pages
            .Where(p => p.Slug == request.Slug)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var pageEntity = list.SingleOrDefault();
        return pageEntity is null
            ? _msg.NotFound<PublicPageDto>(MessageKeys.Content.PAGE_NOT_FOUND)
            : _msg.Ok(MapToDto(pageEntity), MessageKeys.General.SUCCESS_OPERATION);
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
