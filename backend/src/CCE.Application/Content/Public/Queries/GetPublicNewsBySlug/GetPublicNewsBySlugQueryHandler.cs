using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using CCE.Application.Content.Public.Queries.ListPublicNews;
using MediatR;

namespace CCE.Application.Content.Public.Queries.GetPublicNewsBySlug;

public sealed class GetPublicNewsBySlugQueryHandler : IRequestHandler<GetPublicNewsBySlugQuery, PublicNewsDto?>
{
    private readonly ICceDbContext _db;

    public GetPublicNewsBySlugQueryHandler(ICceDbContext db)
    {
        _db = db;
    }

    public async Task<PublicNewsDto?> Handle(GetPublicNewsBySlugQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.News
            .Where(n => n.Slug == request.Slug && n.PublishedOn != null)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);

        var news = list.SingleOrDefault();
        return news is null ? null : ListPublicNewsQueryHandler.MapToDto(news);
    }
}
