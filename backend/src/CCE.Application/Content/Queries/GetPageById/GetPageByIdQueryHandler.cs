using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Queries.GetPageById;

public sealed class GetPageByIdQueryHandler : IRequestHandler<GetPageByIdQuery, Response<PageDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _msg;

    public GetPageByIdQueryHandler(ICceDbContext db, MessageFactory msg)
    {
        _db = db;
        _msg = msg;
    }

    public async Task<Response<PageDto>> Handle(GetPageByIdQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.Pages.Where(p => p.Id == request.Id).ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var pageEntity = list.SingleOrDefault();
        return pageEntity is null
            ? _msg.NotFound<PageDto>(MessageKeys.Content.PAGE_NOT_FOUND)
            : _msg.Ok(MapToDto(pageEntity), MessageKeys.General.SUCCESS_OPERATION);
    }

    internal static PageDto MapToDto(Page p) => new(
        p.Id, p.Slug, p.PageType, p.TitleAr, p.TitleEn, p.ContentAr, p.ContentEn,
        System.Convert.ToBase64String(p.RowVersion));
}
