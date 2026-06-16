using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Public.Queries.ListHomepageFeed;

public sealed class ListHomepageFeedQueryHandler
    : IRequestHandler<ListHomepageFeedQuery, Response<PagedResult<HomepageFeedItemDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public ListHomepageFeedQueryHandler(ICceDbContext db, MessageFactory messages)
    {
        _db = db;
        _messages = messages;
    }

    public async Task<Response<PagedResult<HomepageFeedItemDto>>> Handle(
        ListHomepageFeedQuery request, CancellationToken cancellationToken)
    {
        var newsQ = _db.News
            .Where(n => n.PublishedOn != null)
            .Select(n => new FeedRow
            {
                Id = n.Id,
                ContentType = HomepageFeedContentType.News,
                NameEn = n.TitleEn,
                NameAr = n.TitleAr,
                AuthorId = (System.Guid?)n.AuthorId,
                TopicId = n.TopicId,
                PublishedOn = n.PublishedOn!.Value,
                FeaturedImageUrl = n.FeaturedImageUrl,
                LocationEn = null,
                LocationAr = null,
            });

        var eventsQ = _db.Events
            .Select(e => new FeedRow
            {
                Id = e.Id,
                ContentType = HomepageFeedContentType.Event,
                NameEn = e.TitleEn,
                NameAr = e.TitleAr,
                AuthorId = null,
                TopicId = e.TopicId,
                PublishedOn = e.StartsOn,
                FeaturedImageUrl = e.FeaturedImageUrl,
                LocationEn = e.LocationEn,
                LocationAr = e.LocationAr,
            });

        IQueryable<FeedRow> combined = request.ContentType switch
        {
            HomepageFeedContentType.News => newsQ,
            HomepageFeedContentType.Event => eventsQ,
            _ => newsQ.Concat(eventsQ),
        };

        combined = combined.WhereIf(request.TopicId.HasValue, r => r.TopicId == request.TopicId!.Value);

        combined = ApplySort(combined, request.SortBy, request.SortOrder);

        var result = await combined
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);

        var topicIds = result.Items.Select(r => r.TopicId).Distinct().ToList();
        var topicsList = await _db.Topics.Where(t => topicIds.Contains(t.Id))
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var topicById = topicsList.ToDictionary(t => t.Id);

        var authorIds = result.Items
            .Where(r => r.AuthorId.HasValue)
            .Select(r => r.AuthorId!.Value)
            .Distinct()
            .ToList();
        var authorsList = await _db.Users.Where(u => authorIds.Contains(u.Id))
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var authorById = authorsList.ToDictionary(
            u => u.Id,
            u =>
            {
                var fullName = $"{u.FirstName} {u.LastName}".Trim();
                return string.IsNullOrEmpty(fullName) ? u.UserName ?? string.Empty : fullName;
            });

        return _messages.Ok(result.Map(r => MapToDto(r, topicById, authorById)), "ITEMS_LISTED");
    }

    private static IQueryable<FeedRow> ApplySort(
        IQueryable<FeedRow> query,
        HomepageFeedSortBy sortBy,
        SortOrder sortOrder)
    {
        return sortBy switch
        {
            HomepageFeedSortBy.Date => sortOrder == SortOrder.Ascending
                ? query.OrderBy(r => r.PublishedOn)
                : query.OrderByDescending(r => r.PublishedOn),
            _ => query.OrderByDescending(r => r.PublishedOn),
        };
    }

    private static HomepageFeedItemDto MapToDto(
        FeedRow r,
        System.Collections.Generic.Dictionary<System.Guid, Topic> topicById,
        System.Collections.Generic.Dictionary<System.Guid, string> authorById) => new(
        r.Id,
        (int)r.ContentType,
        r.ContentType,
        r.NameEn,
        r.NameAr,
        r.TopicId,
        topicById.TryGetValue(r.TopicId, out var t) ? t.NameEn : string.Empty,
        topicById.TryGetValue(r.TopicId, out t) ? t.NameAr : string.Empty,
        r.AuthorId,
        r.AuthorId.HasValue && authorById.TryGetValue(r.AuthorId.Value, out var name) ? name : null,
        r.FeaturedImageUrl,
        r.LocationEn,
        r.LocationAr,
        r.PublishedOn);
}
