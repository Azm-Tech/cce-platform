using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
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
    private readonly IUserContentInterestResolver _resolver;

    public ListPublicEventsQueryHandler(ICceDbContext db, MessageFactory messages, IUserContentInterestResolver resolver)
    {
        _db = db;
        _messages = messages;
        _resolver = resolver;
    }

    public async Task<Response<PagedResult<PublicEventDto>>> Handle(ListPublicEventsQuery request, CancellationToken cancellationToken)
    {
        var knowledgeLevelId = request.KnowledgeLevelId;
        var jobSectorId = request.JobSectorId;

        (knowledgeLevelId, jobSectorId) = await _resolver.ResolveAsync(knowledgeLevelId, jobSectorId, cancellationToken).ConfigureAwait(false);

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

        query = query.WhereIf(request.TopicId.HasValue, e => e.TopicId == request.TopicId!.Value);
        query = query.WhereIf(request.TagIds?.Count > 0, e => e.Tags.Any(t => request.TagIds!.Contains(t.Id)));

        if (knowledgeLevelId.HasValue || jobSectorId.HasValue)
        {
            query = query.Where(e =>
                (!knowledgeLevelId.HasValue || e.KnowledgeLevelId == null || e.KnowledgeLevelId == knowledgeLevelId.Value) &&
                (!jobSectorId.HasValue || e.JobSectorId == null || e.JobSectorId == jobSectorId.Value));

            query = query.OrderByDescending(e =>
                (knowledgeLevelId.HasValue && e.KnowledgeLevelId == knowledgeLevelId.Value ? 2 : 0) +
                (jobSectorId.HasValue && e.JobSectorId == jobSectorId.Value ? 1 : 0))
                .ThenBy(e => e.StartsOn);
        }
        else
        {
            query = query.OrderBy(e => e.StartsOn);
        }

        var result = await query.ToPagedResultAsync(request.Page, request.PageSize, cancellationToken).ConfigureAwait(false);

        var topicIds = result.Items.Select(e => e.TopicId).Distinct().ToList();
        var topicsList = await _db.Topics.Where(t => topicIds.Contains(t.Id))
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var topicById = topicsList.ToDictionary(t => t.Id);

        var eventIds = result.Items.Select(e => e.Id).ToList();
        var tagByEventId = await GetTagDtosByEventIdsAsync(eventIds, cancellationToken).ConfigureAwait(false);

        return _messages.Ok(result.Map(e => MapToDto(e, topicById, tagByEventId)), MessageKeys.General.ITEMS_LISTED);
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

    internal static PublicEventDto MapToDto(Event e, Dictionary<System.Guid, Topic> topicById, Dictionary<System.Guid, List<TagDto>> tagByEventId) => new(
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
        topicById.TryGetValue(e.TopicId, out t) ? t.NameEn : string.Empty,
        tagByEventId.TryGetValue(e.Id, out var tags) ? tags : new List<TagDto>(),
        e.KnowledgeLevelId,
        e.JobSectorId);

}
