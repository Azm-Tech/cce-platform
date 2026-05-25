using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.ListEvents;
using MediatR;

namespace CCE.Application.Content.Commands.RescheduleEvent;

public sealed class RescheduleEventCommandHandler : IRequestHandler<RescheduleEventCommand, EventDto?>
{
    private readonly IEventRepository _service;

    public RescheduleEventCommandHandler(IEventRepository service)
    {
        _service = service;
    }

    public async Task<EventDto?> Handle(RescheduleEventCommand request, CancellationToken cancellationToken)
    {
        var ev = await _service.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (ev is null)
        {
            return null;
        }

        ev.Reschedule(request.StartsOn, request.EndsOn);

        await _service.UpdateAsync(ev, request.RowVersion, cancellationToken).ConfigureAwait(false);

        return ListEventsQueryHandler.MapToDto(ev);
    }
}
