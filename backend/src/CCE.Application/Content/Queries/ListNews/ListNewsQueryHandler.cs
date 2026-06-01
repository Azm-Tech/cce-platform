using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Community;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Queries.ListNews;

public sealed class ListNewsQueryHandler : IRequestHandler<ListNewsQuery, Response<PagedResult<NewsDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public ListNewsQueryHandler(ICceDbContext db, MessageFactory messages)
    {
        _db = db;
        _messages = messages;
    }

    public async Task<Response<PagedResult<NewsDto>>> Handle(ListNewsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.News
            .WhereIf(!string.IsNullOrWhiteSpace(request.Search),
                n => n.TitleAr.Contains(request.Search!) ||
                     n.TitleEn.Contains(request.Search!) ||
                     n.Slug.Contains(request.Search!))
            .WhereIf(request.IsPublished == true,  n => n.PublishedOn != null)
            .WhereIf(request.IsPublished == false, n => n.PublishedOn == null)
            .WhereIf(request.IsFeatured.HasValue,  n => n.IsFeatured == request.IsFeatured!.Value)
            .WhereIf(request.TopicId.HasValue, n => n.TopicId == request.TopicId!.Value)
            .OrderByDescending(n => n.PublishedOn ?? DateTimeOffset.MinValue)
            .ThenByDescending(n => n.Id);

        var result = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken).ConfigureAwait(false);

        var topicIds = result.Items.Select(n => n.TopicId).Distinct().ToList();
        var topics = await _db.Topics.Where(t => topicIds.Contains(t.Id))
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var topicById = topics.ToDictionary(t => t.Id);

        return _messages.Ok(result.Map(n => MapToDto(n, topicById)), "ITEMS_LISTED");
    }

    internal static NewsDto MapToDto(News n, Dictionary<System.Guid, Topic> topicById) => new(
        n.Id, n.TitleAr, n.TitleEn, n.ContentAr, n.ContentEn,
        n.TopicId,
        topicById.TryGetValue(n.TopicId, out var t) ? t.NameAr : string.Empty,
        topicById.TryGetValue(n.TopicId, out t) ? t.NameEn : string.Empty,
        n.AuthorId, n.FeaturedImageUrl,
        n.PublishedOn, n.IsFeatured, n.IsPublished);

    internal static NewsDto MapToDto(News n, string topicNameAr = "", string topicNameEn = "") => new(
        n.Id, n.TitleAr, n.TitleEn, n.ContentAr, n.ContentEn,
        n.TopicId, topicNameAr, topicNameEn,
        n.AuthorId, n.FeaturedImageUrl,
        n.PublishedOn, n.IsFeatured, n.IsPublished);
}
