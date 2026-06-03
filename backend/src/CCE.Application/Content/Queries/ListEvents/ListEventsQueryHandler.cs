using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Messages;
using CCE.Domain.Community;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Queries.ListEvents;

public sealed class ListEventsQueryHandler : IRequestHandler<ListEventsQuery, Response<PagedResult<EventDto>>>
{
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public ListEventsQueryHandler(ICceDbContext db, MessageFactory messages)
    {
        _db = db;
        _messages = messages;
    }

    public async Task<Response<PagedResult<EventDto>>> Handle(ListEventsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Events
            .WhereIf(!string.IsNullOrWhiteSpace(request.Search),
                e => e.TitleAr.Contains(request.Search!) ||
                     e.TitleEn.Contains(request.Search!))
            .WhereIf(request.FromDate.HasValue, e => e.StartsOn >= request.FromDate!.Value)
            .WhereIf(request.ToDate.HasValue,   e => e.EndsOn <= request.ToDate!.Value)
            .WhereIf(request.TopicId.HasValue, e => e.TopicId == request.TopicId!.Value)
            .WhereIf(request.TagIds?.Count > 0, e => e.Tags.Any(t => request.TagIds!.Contains(t.Id)))
            .OrderByDescending(e => e.StartsOn);

        var result = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken).ConfigureAwait(false);

        var topicIds = result.Items.Select(e => e.TopicId).Distinct().ToList();
        var topics = await _db.Topics.Where(t => topicIds.Contains(t.Id))
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var topicById = topics.ToDictionary(t => t.Id);

        var eventIds = result.Items.Select(e => e.Id).ToList();
        var tagByEventId = await GetTagDtosByEventIdsAsync(eventIds, cancellationToken).ConfigureAwait(false);

        return _messages.Ok(result.Map(e => MapToDto(e, topicById, tagByEventId)), "ITEMS_LISTED");
    }

    private async Task<Dictionary<System.Guid, List<TagDto>>> GetTagDtosByEventIdsAsync(
        System.Collections.Generic.List<System.Guid> eventIds, CancellationToken ct)
    {
        if (eventIds.Count == 0)
            return new Dictionary<System.Guid, List<TagDto>>();

        var entries = await _db.Events
            .Where(e => eventIds.Contains(e.Id))
            .Select(e => new { e.Id, Tags = e.Tags.Select(t => new TagDto(t.Id, t.NameAr, t.NameEn, t.Color)).ToList() })
            .ToListAsyncEither(ct).ConfigureAwait(false);

        return entries.ToDictionary(x => x.Id, x => x.Tags);
    }

    internal static EventDto MapToDto(Event e, Dictionary<System.Guid, Topic> topicById, Dictionary<System.Guid, List<TagDto>> tagByEventId) => new(
        e.Id, e.TitleAr, e.TitleEn, e.DescriptionAr, e.DescriptionEn,
        e.StartsOn, e.EndsOn, e.LocationAr, e.LocationEn,
        e.OnlineMeetingUrl, e.FeaturedImageUrl, e.ICalUid,
        e.TopicId,
        topicById.TryGetValue(e.TopicId, out var t) ? t.NameAr : string.Empty,
        topicById.TryGetValue(e.TopicId, out t) ? t.NameEn : string.Empty,
        tagByEventId.TryGetValue(e.Id, out var tags) ? tags : new List<TagDto>());

    internal static EventDto MapToDto(Event e, string topicNameAr = "", string topicNameEn = "", System.Collections.Generic.IReadOnlyList<TagDto>? tags = null) => new(
        e.Id, e.TitleAr, e.TitleEn, e.DescriptionAr, e.DescriptionEn,
        e.StartsOn, e.EndsOn, e.LocationAr, e.LocationEn,
        e.OnlineMeetingUrl, e.FeaturedImageUrl, e.ICalUid,
        e.TopicId, topicNameAr, topicNameEn,
        tags ?? new List<TagDto>());
}
