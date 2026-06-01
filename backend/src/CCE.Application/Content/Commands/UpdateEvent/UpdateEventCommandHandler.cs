using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.GetEventById;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using MediatR;

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
        var ev = await _repo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (ev is null)
            return _messages.EventNotFound<EventDto>();

        var topicExists = await _db.Topics.Where(t => t.Id == request.TopicId).CountAsyncEither(cancellationToken) > 0;
        if (!topicExists)
            return _messages.NotFound<EventDto>("TOPIC_NOT_FOUND");

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
            request.TopicId);

        _db.SetExpectedRowVersion(ev, expectedRowVersion);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var topic = await _db.Topics.Where(t => t.Id == request.TopicId)
            .ToListAsyncEither(cancellationToken).ConfigureAwait(false);
        var topicNameAr = topic.FirstOrDefault()?.NameAr ?? string.Empty;
        var topicNameEn = topic.FirstOrDefault()?.NameEn ?? string.Empty;

        return _messages.Ok(GetEventByIdQueryHandler.MapToDto(ev, topicNameAr, topicNameEn), "SUCCESS_OPERATION");
    }
}
