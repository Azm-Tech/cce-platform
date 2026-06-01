using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Public.Queries.GetPublicNewsBySlug;

public sealed class GetPublicNewsBySlugQueryHandler : IRequestHandler<GetPublicNewsBySlugQuery, Response<PublicNewsDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public GetPublicNewsBySlugQueryHandler(ICceDbContext db, MessageFactory messages)
    {
        _db = db;
        _messages = messages;
    }

    public async Task<Response<PublicNewsDto>> Handle(GetPublicNewsBySlugQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.News
            .Where(n => n.Slug == request.Slug && n.PublishedOn != null)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var news = list.SingleOrDefault();
        if (news is null)
            return _messages.NewsNotFound<PublicNewsDto>();

        var topics = await _db.Topics.Where(t => t.Id == news.TopicId)
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var topic = topics.FirstOrDefault();

        return _messages.Ok(MapToDto(news, topic?.NameAr ?? string.Empty, topic?.NameEn ?? string.Empty), "SUCCESS_OPERATION");
    }

    internal static PublicNewsDto MapToDto(News n, string topicNameAr, string topicNameEn) => new(
        n.Id,
        n.TitleAr,
        n.TitleEn,
        n.ContentAr,
        n.ContentEn,
        n.TopicId,
        topicNameAr,
        topicNameEn,
        n.FeaturedImageUrl,
        n.PublishedOn!.Value,
        n.IsFeatured);
}
