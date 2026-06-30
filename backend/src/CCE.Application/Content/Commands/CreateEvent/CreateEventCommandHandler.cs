using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.ListEvents;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.CreateEvent;

public sealed class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, Response<EventDto>>
{
    private readonly IRepository<Event, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly ISystemClock _clock;
    private readonly MessageFactory _messages;

    public CreateEventCommandHandler(
        IRepository<Event, System.Guid> repo,
        ICceDbContext db,
        ISystemClock clock,
        MessageFactory messages)
    {
        _repo = repo;
        _db = db;
        _clock = clock;
        _messages = messages;
    }

    public async Task<Response<EventDto>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var topicExists = await _db.Topics.Where(t => t.Id == request.TopicId).CountAsyncEither(cancellationToken) > 0;
        if (!topicExists)
            return _messages.NotFound<EventDto>(MessageKeys.Community.TOPIC_NOT_FOUND);

        var ev = Event.Schedule(
            request.TitleAr,
            request.TitleEn,
            request.DescriptionAr,
            request.DescriptionEn,
            request.StartsOn,
            request.EndsOn,
            request.LocationAr,
            request.LocationEn,
            request.OnlineMeetingUrl,
            request.FeaturedImageUrl,
            request.TopicId,
            _clock,
            request.KnowledgeLevelId,
            request.JobSectorId);

        if (request.TagIds?.Count > 0)
        {
            var tags = await _db.Tags.Where(t => request.TagIds.Contains(t.Id))
                .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
            // Tags load detached (ICceDbContext exposes DbSets AsNoTracking). Attach as Unchanged so
            // EF only writes the event_tag join rows instead of INSERTing existing tags (PK violation).
            foreach (var tag in tags) _db.Attach(tag);
            ev.SetTags(tags);
        }

        await _repo.AddAsync(ev, cancellationToken).ConfigureAwait(false);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var topic = await _db.Topics.Where(t => t.Id == request.TopicId)
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var topicNameAr = topic.FirstOrDefault()?.NameAr ?? string.Empty;
        var topicNameEn = topic.FirstOrDefault()?.NameEn ?? string.Empty;

        return _messages.Ok(ListEventsQueryHandler.MapToDto(ev, topicNameAr, topicNameEn, ev.Tags.Select(t => new TagDto(t.Id, t.NameAr, t.NameEn, t.Color)).ToList()), MessageKeys.Content.CONTENT_CREATED);
    }
}
