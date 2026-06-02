using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Public.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Community;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Public.Queries.ListPublicEvents;

public sealed class ListPublicEventsQueryHandler : IRequestHandler<ListPublicEventsQuery, Response<PagedResult<PublicEventDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public ListPublicEventsQueryHandler(ICceDbContext db, MessageFactory messages)
    {
        _db = db;
        _messages = messages;
    }

    public async Task<Response<PagedResult<PublicEventDto>>> Handle(ListPublicEventsQuery request, CancellationToken cancellationToken)
    {
        System.Guid? topicId = request.TopicId;
        if (!string.IsNullOrWhiteSpace(request.TopicSlug) && !topicId.HasValue)
        {
            var topics = await _db.Topics.Where(t => t.Slug == request.TopicSlug!)
                .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
            topicId = topics.FirstOrDefault()?.Id;
        }

        var query = _db.Events.AsQueryable();

        if (request.From.HasValue && request.To.HasValue)
        {
            query = query.Where(e => e.StartsOn >= request.From.Value && e.StartsOn <= request.To.Value);
        }
        else
        {
            var now = DateTimeOffset.UtcNow;
            query = query.Where(e => e.StartsOn >= now);
        }

        query = query.WhereIf(topicId.HasValue, e => e.TopicId == topicId!.Value);
        query = ApplySort(query, request.SortBy, request.SortOrder);

        var result = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken).ConfigureAwait(false);

        var topicIds = result.Items.Select(e => e.TopicId).Distinct().ToList();
        var topicsList = await _db.Topics.Where(t => topicIds.Contains(t.Id))
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var topicById = topicsList.ToDictionary(t => t.Id);

        return _messages.Ok(result.Map(e => MapToDto(e, topicById)), "ITEMS_LISTED");
    }

    private static IQueryable<Event> ApplySort(IQueryable<Event> query, EventSortBy sortBy, SortOrder sortOrder)
    {
        return sortBy switch
        {
            EventSortBy.Date => sortOrder == SortOrder.Ascending
                ? query.OrderBy(e => e.StartsOn)
                : query.OrderByDescending(e => e.StartsOn),
            _ => query.OrderByDescending(e => e.StartsOn),
        };
    }

    internal static PublicEventDto MapToDto(Event e, Dictionary<System.Guid, Topic> topicById) => new(
        e.Id,
        e.TitleAr,
        e.TitleEn,
        e.DescriptionAr,
        e.DescriptionEn,
        e.StartsOn,
        e.EndsOn,
        e.LocationAr,
        e.LocationEn,
        e.OnlineMeetingUrl,
        e.FeaturedImageUrl,
        e.ICalUid,
        e.TopicId,
        topicById.TryGetValue(e.TopicId, out var t) ? t.NameAr : string.Empty,
        topicById.TryGetValue(e.TopicId, out t) ? t.NameEn : string.Empty);
}
