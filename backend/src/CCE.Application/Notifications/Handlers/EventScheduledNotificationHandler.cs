using CCE.Domain.Content.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CCE.Application.Notifications.Handlers;

public sealed class EventScheduledNotificationHandler
    : INotificationHandler<EventScheduledEvent>
{
    private readonly ILogger<EventScheduledNotificationHandler> _logger;

    public EventScheduledNotificationHandler(
        ILogger<EventScheduledNotificationHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(EventScheduledEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Event {EventId} scheduled. Audience notifications require explicit audience definition.",
            notification.EventId);
        return Task.CompletedTask;
    }
}
