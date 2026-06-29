using CCE.Application.Common.Messaging.IntegrationEvents;
using CCE.Application.Notifications.Messages;
using CCE.Domain.Notifications;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Notifications.Messaging.Consumers;

/// <summary>
/// Sends the content author a persistent in-app notification when their post or reply is taken
/// down by moderation (<see cref="ContentRejectedIntegrationEvent"/>). Fires only on an actual
/// takedown — never on a still-visible <c>Flagged</c> outcome — and uses generic copy so the
/// AI category/score isn't leaked to the author.
/// </summary>
public sealed class ContentRejectedAuthorNotificationConsumer : IConsumer<ContentRejectedIntegrationEvent>
{
    private const string TemplateCode = "CONTENT_REJECTED";

    private readonly INotificationMessageDispatcher _dispatcher;
    private readonly ILogger<ContentRejectedAuthorNotificationConsumer> _logger;

    public ContentRejectedAuthorNotificationConsumer(
        INotificationMessageDispatcher dispatcher,
        ILogger<ContentRejectedAuthorNotificationConsumer> logger)
    {
        _dispatcher = dispatcher;
        _logger     = logger;
    }

    public async Task Consume(ConsumeContext<ContentRejectedIntegrationEvent> context)
    {
        var evt = context.Message;

        await _dispatcher.DispatchAsync(new NotificationMessage(
            TemplateCode: TemplateCode,
            RecipientUserId: evt.AuthorId,
            EventType: NotificationEventType.CommunityContentRejected,
            Channels: new[] { NotificationChannel.InApp },
            Locale: string.IsNullOrWhiteSpace(evt.Locale) ? "en" : evt.Locale),
            context.CancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "ContentRejectedAuthorNotificationConsumer: notified author {AuthorId} that {ContentType} {ContentId} was removed",
            evt.AuthorId, evt.ContentType, evt.ContentId);
    }
}
