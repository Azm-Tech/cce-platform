using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Content.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Public.Queries.GetPublicNewsById;

public sealed class GetPublicNewsByIdQueryHandler : IRequestHandler<GetPublicNewsByIdQuery, Response<PublicNewsDto>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public GetPublicNewsByIdQueryHandler(ICceDbContext db, MessageFactory messages)
    {
        _db = db;
        _messages = messages;
    }

    public async Task<Response<PublicNewsDto>> Handle(GetPublicNewsByIdQuery request, CancellationToken cancellationToken)
    {
        var list = await _db.News
            .Where(n => n.Id == request.Id && n.PublishedOn != null)
            .ToListAsyncEither(cancellationToken)
            .ConfigureAwait(false);
        var news = list.SingleOrDefault();
        if (news is null)
            return _messages.NotFound<PublicNewsDto>(MessageKeys.Content.NEWS_NOT_FOUND);

        var topics = await _db.Topics.Where(t => t.Id == news.TopicId)
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var topic = topics.FirstOrDefault();

        var tagDtos = news.Tags.Select(t => new TagDto(t.Id, t.NameAr, t.NameEn, t.Color)).ToList();

        return _messages.Ok(MapToDto(news, topic?.NameAr ?? string.Empty, topic?.NameEn ?? string.Empty, tagDtos), MessageKeys.General.SUCCESS_OPERATION);
    }

    internal static PublicNewsDto MapToDto(News n, string topicNameAr, string topicNameEn, System.Collections.Generic.IReadOnlyList<TagDto>? tags = null) => new(
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
        n.IsFeatured,
        tags ?? new List<TagDto>(),
        n.KnowledgeLevelId,
        n.JobSectorId);
}
