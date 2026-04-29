using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.ListPages;
using MediatR;

namespace CCE.Application.Content.Queries.GetPageById;

public sealed class GetPageByIdQueryHandler : IRequestHandler<GetPageByIdQuery, PageDto?>
{
    private readonly ICceDbContext _db;

    public GetPageByIdQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<PageDto?> Handle(GetPageByIdQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.Pages.Where(p => p.Id == request.Id).ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var page = list.SingleOrDefault();
        return page is null ? null : ListPagesQueryHandler.MapToDto(page);
    }
}
