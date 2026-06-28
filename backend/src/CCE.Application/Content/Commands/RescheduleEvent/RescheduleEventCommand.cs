using CCE.Application.Common;
using CCE.Application.Content.Dtos;
using MediatR;

namespace CCE.Application.Content.Commands.RescheduleEvent;

public sealed record RescheduleEventCommand(
    System.Guid Id,
    System.DateTimeOffset StartsOn,
    System.DateTimeOffset EndsOn) : IRequest<Response<EventDto>>;
