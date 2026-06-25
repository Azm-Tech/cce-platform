using CCE.Application.Common;
using CCE.Application.Common.Interfaces;
using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.GetEventById;
using CCE.Application.Messages;
using CCE.Domain.Common;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.RescheduleEvent;

public sealed class RescheduleEventCommandHandler : IRequestHandler<RescheduleEventCommand, Response<EventDto>>
{
    private readonly IRepository<Event, System.Guid> _repo;
    private readonly ICceDbContext _db;
    private readonly MessageFactory _messages;

    public RescheduleEventCommandHandler(
        IRepository<Event, System.Guid> repo,
        ICceDbContext db,
        MessageFactory messages)
    {
        _repo = repo;
        _db = db;
        _messages = messages;
    }

    public async Task<Response<EventDto>> Handle(RescheduleEventCommand request, CancellationToken cancellationToken)
    {
        var ev = await _repo.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (ev is null)
            return _messages.NotFound<EventDto>(MessageKeys.Content.EVENT_NOT_FOUND);

        var expectedRowVersion = ev.RowVersion;
        ev.Reschedule(request.StartsOn, request.EndsOn);

        _db.SetExpectedRowVersion(ev, expectedRowVersion);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return _messages.Ok(GetEventByIdQueryHandler.MapToDto(ev), MessageKeys.General.SUCCESS_OPERATION);
    }
}
