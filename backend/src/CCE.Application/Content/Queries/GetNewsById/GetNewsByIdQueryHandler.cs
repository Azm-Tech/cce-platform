using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Community;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Queries.GetNewsById;

public sealed class GetNewsByIdQueryHandler : IRequestHandler<GetNewsByIdQuery, Response<NewsDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public GetNewsByIdQueryHandler(ICceDbContext db, MessageFactory messages)
    {
        _db = db;
        _messages = messages;
    }

    public async Task<Response<NewsDto>> Handle(GetNewsByIdQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.News.Where(n => n.Id == request.Id).ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var news = list.SingleOrDefault();
        if (news is null)
            return _messages.NewsNotFound<NewsDto>();

        var topics = await _db.Topics.Where(t => t.Id == news.TopicId)
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var topic = topics.FirstOrDefault();

        var tagDtos = news.Tags.Select(t => new TagDto(t.Id, t.NameAr, t.NameEn, t.Color)).ToList();

        return _messages.Ok(MapToDto(news, topic?.NameAr ?? string.Empty, topic?.NameEn ?? string.Empty, tagDtos), "SUCCESS_OPERATION");
    }

    internal static NewsDto MapToDto(News n, string topicNameAr = "", string topicNameEn = "", System.Collections.Generic.IReadOnlyList<TagDto>? tags = null) => new(
        n.Id, n.TitleAr, n.TitleEn, n.ContentAr, n.ContentEn,
        n.TopicId, topicNameAr, topicNameEn,
        n.AuthorId, n.FeaturedImageUrl,
        n.PublishedOn, n.IsFeatured, n.IsPublished,
        tags ?? new List<TagDto>());
}
