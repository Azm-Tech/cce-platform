using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Community;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Public.Queries.ListPublicNews;

public sealed class ListPublicNewsQueryHandler : IRequestHandler<ListPublicNewsQuery, Response<PagedResult<PublicNewsDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public ListPublicNewsQueryHandler(ICceDbContext db, MessageFactory messages)
    {
        _db = db;
        _messages = messages;
    }

    public async Task<Response<PagedResult<PublicNewsDto>>> Handle(ListPublicNewsQuery request, CancellationToken cancellationToken)
    {
        System.Guid? topicId = request.TopicId;
        if (!string.IsNullOrWhiteSpace(request.TopicSlug) && !topicId.HasValue)
        {
            var topics = await _db.Topics.Where(t => t.Slug == request.TopicSlug!)
                .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
            topicId = topics.FirstOrDefault()?.Id;
        }

        var query = _db.News
            .Where(n => n.PublishedOn != null)
            .WhereIf(request.IsFeatured.HasValue, n => n.IsFeatured == request.IsFeatured!.Value)
            .WhereIf(topicId.HasValue, n => n.TopicId == topicId!.Value)
            .OrderByDescending(n => n.PublishedOn);

        var result = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken).ConfigureAwait(false);

        var topicIds = result.Items.Select(n => n.TopicId).Distinct().ToList();
        var topicsList = await _db.Topics.Where(t => topicIds.Contains(t.Id))
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var topicById = topicsList.ToDictionary(t => t.Id);

        return _messages.Ok(result.Map(n => MapToDto(n, topicById)), "ITEMS_LISTED");
    }

    internal static PublicNewsDto MapToDto(News n, Dictionary<System.Guid, Topic> topicById) => new(
        n.Id,
        n.TitleAr,
        n.TitleEn,
        n.ContentAr,
        n.ContentEn,
        n.TopicId,
        topicById.TryGetValue(n.TopicId, out var t) ? t.NameAr : string.Empty,
        topicById.TryGetValue(n.TopicId, out t) ? t.NameEn : string.Empty,
        n.FeaturedImageUrl,
        n.PublishedOn!.Value,
        n.IsFeatured);
}
