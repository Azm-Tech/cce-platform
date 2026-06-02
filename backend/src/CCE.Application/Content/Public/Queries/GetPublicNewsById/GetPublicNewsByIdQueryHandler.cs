using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Community;
using CCE.Domain.Content;
using CCE.Domain.Identity;
using MediatR;

namespace CCE.Application.Content.Public.Queries.GetPublicNewsById;

public sealed class GetPublicNewsByIdQueryHandler : IRequestHandler<GetPublicNewsByIdQuery, Response<NewsDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public GetPublicNewsByIdQueryHandler(ICceDbContext db, MessageFactory messages)
    {
        _db = db;
        _messages = messages;
    }

    public async Task<Response<NewsDto>> Handle(GetPublicNewsByIdQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.News.Where(n => n.Id == request.Id).ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var news = list.SingleOrDefault();
        if (news is null)
            return _messages.NewsNotFound<NewsDto>();

        var topics = await _db.Topics.Where(t => t.Id == news.TopicId)
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var topic = topics.FirstOrDefault();

        var users = await _db.Users.Where(u => u.Id == news.AuthorId)
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var author = users.FirstOrDefault();
        var authorName = author is not null ? $"{author.FirstName} {author.LastName}".Trim() : string.Empty;

        return _messages.Ok(MapToDto(news, topic?.NameAr ?? string.Empty, topic?.NameEn ?? string.Empty, authorName), "SUCCESS_OPERATION");
    }

    internal static NewsDto MapToDto(News n, string topicNameAr = "", string topicNameEn = "", string authorName = "") => new(
        n.Id, n.TitleAr, n.TitleEn, n.ContentAr, n.ContentEn,
        n.TopicId, topicNameAr, topicNameEn,
        n.AuthorId, authorName, n.FeaturedImageUrl,
        n.PublishedOn, n.IsFeatured, n.IsPublished);
}
