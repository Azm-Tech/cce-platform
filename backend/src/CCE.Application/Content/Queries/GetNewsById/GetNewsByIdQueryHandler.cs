using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Queries.GetNewsById;

public sealed class GetNewsByIdQueryHandler : IRequestHandler<GetNewsByIdQuery, NewsDto?>
{
    private readonly ICceDbContext _db;

    public GetNewsByIdQueryHandler(ICceDbContext db) => _db = db;

    public async Task<NewsDto?> Handle(GetNewsByIdQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.News.Where(n => n.Id == request.Id).ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var news = list.SingleOrDefault();
        return news is null ? null : MapToDto(news);
    }

    internal static NewsDto MapToDto(News n) => new(
        n.Id, n.TitleAr, n.TitleEn, n.ContentAr, n.ContentEn,
        n.Slug, n.AuthorId, n.FeaturedImageUrl,
        n.PublishedOn, n.IsFeatured, n.IsPublished,
        System.Convert.ToBase64String(n.RowVersion));
}
