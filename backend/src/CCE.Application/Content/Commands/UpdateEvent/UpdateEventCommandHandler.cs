using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.ListEvents;
using MediatR;

namespace CCE.Application.Content.Commands.UpdateEvent;

public sealed class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand, EventDto?>
{
    private readonly IEventService _service;

    public UpdateEventCommandHandler(IEventService service)
    {
        _service = service;
    }

    public async Task<EventDto?> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        var ev = await _service.FindAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (ev is null)
        {
            return null;
        }

        ev.UpdateContent(
            request.TitleAr,
            request.TitleEn,
            request.DescriptionAr,
            request.DescriptionEn,
            request.LocationAr,
            request.LocationEn,
            request.OnlineMeetingUrl,
            request.FeaturedImageUrl);

        await _service.UpdateAsync(ev, request.RowVersion, cancellationToken).ConfigureAwait(false);

        return ListEventsQueryHandler.MapToDto(ev);
    }
}
