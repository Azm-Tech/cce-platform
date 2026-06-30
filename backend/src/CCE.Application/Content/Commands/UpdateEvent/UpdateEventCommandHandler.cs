using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.GetEventById;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Content.Commands.UpdateEvent;

public sealed class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand, Response<EventDto>>
{
    private readonly IRepository<Event, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public UpdateEventCommandHandler(
        IRepository<Event, System.Guid> repo,
        ICceDbContext db,
        MessageFactory messages)
    {
        _repo = repo;
        _db = db;
        _messages = messages;
    }

    public async Task<Response<EventDto>> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        var ev = await _repo.GetByIdAsync(
            request.Id,
            q => q.Include(e => e.Tags),
            cancellationToken).ConfigureAwait(false);
        if (ev is null)
            return _messages.NotFound<EventDto>(MessageKeys.Content.EVENT_NOT_FOUND);

        var topicExists = await _db.Topics.Where(t => t.Id == request.TopicId).CountAsyncEither(cancellationToken) > 0;
        if (!topicExists)
            return _messages.NotFound<EventDto>(MessageKeys.Community.TOPIC_NOT_FOUND);

        var expectedRowVersion = ev.RowVersion;
        ev.UpdateContent(
            request.TitleAr,
            request.TitleEn,
            request.DescriptionAr,
            request.DescriptionEn,
            request.LocationAr,
            request.LocationEn,
            request.OnlineMeetingUrl,
            request.FeaturedImageUrl,
            request.TopicId,
            request.KnowledgeLevelId,
            request.JobSectorId);

        if (request.TagIds is not null)
        {
            var requested = await _db.Tags.Where(t => request.TagIds.Contains(t.Id))
                .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
            // Reuse tag instances already tracked via Include(e => e.Tags); attach the rest as
            // Unchanged so EF writes only event_tag rows (avoids pk_tags INSERT and double-tracking).
            var current = ev.Tags.ToDictionary(t => t.Id);
            var resolved = new System.Collections.Generic.List<Tag>(requested.Count);
            foreach (var tag in requested)
            {
                if (current.TryGetValue(tag.Id, out var tracked)) { resolved.Add(tracked); }
                else { _db.Attach(tag); resolved.Add(tag); }
            }
            ev.SetTags(resolved);
        }

        _db.SetExpectedRowVersion(ev, expectedRowVersion);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var topic = await _db.Topics.Where(t => t.Id == request.TopicId)
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var topicNameAr = topic.FirstOrDefault()?.NameAr ?? string.Empty;
        var topicNameEn = topic.FirstOrDefault()?.NameEn ?? string.Empty;

        var tagDtos = ev.Tags.Select(t => new TagDto(t.Id, t.NameAr, t.NameEn, t.Color)).ToList();
        return _messages.Ok(GetEventByIdQueryHandler.MapToDto(ev, topicNameAr, topicNameEn, tagDtos), MessageKeys.General.SUCCESS_OPERATION);
    }
}
