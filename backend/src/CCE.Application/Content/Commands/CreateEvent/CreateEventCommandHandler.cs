using CCE.Application.Content.Dtos;
using CCE.Application.Content.Queries.ListEvents;
using CCE.Domain.Common;
using CCE.Domain.Content;
using MediatR;

namespace CCE.Application.Content.Commands.CreateEvent;

public sealed class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, EventDto>
{
    private readonly IEventService _service;
    private readonly ISystemClock _clock;

    public CreateEventCommandHandler(IEventService service, ISystemClock clock)
    {
        _service = service;
        _clock = clock;
    }

    public async Task<EventDto> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
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
            _clock);

        await _service.SaveAsync(ev, cancellationToken).ConfigureAwait(false);

        return ListEventsQueryHandler.MapToDto(ev);
    }
}
