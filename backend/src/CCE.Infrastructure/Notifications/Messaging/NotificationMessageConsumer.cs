using CCE.Application.Notifications;
using CCE.Application.Notifications.Messages;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Notifications.Messaging;

/// <summary>
/// MassTransit consumer that receives a <see cref="NotificationMessage"/> from
/// the bus and hands it to <see cref="INotificationGateway"/> for template
/// resolution, rendering, delivery and logging.
///
/// <para>
/// This is the async counterpart to <see cref="InProcessNotificationMessageDispatcher"/>.
/// The gateway call (and its DB + SMS/Email provider I/O) happens here, off the
/// original HTTP request thread.
/// </para>
///
/// <para>
/// Retry policy is configured on the consumer definition
/// (<see cref="NotificationMessageConsumerDefinition"/>): 3 immediate retries,
/// then messages move to the error queue for manual inspection.
/// </para>
/// </summary>
public sealed class NotificationMessageConsumer : IConsumer<NotificationMessage>
{
    private readonly INotificationGateway _gateway;
    private readonly ILogger<NotificationMessageConsumer> _logger;

    public NotificationMessageConsumer(
        INotificationGateway gateway,
        ILogger<NotificationMessageConsumer> logger)
    {
        _gateway = gateway;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<NotificationMessage> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Consuming NotificationMessage TemplateCode={TemplateCode} RecipientUserId={RecipientUserId}",
            message.TemplateCode,
            message.RecipientUserId);

        var result = await _gateway.SendAsync(new NotificationDispatchRequest(
            TemplateCode:    message.TemplateCode,
            RecipientUserId: message.RecipientUserId,
            Channels:        message.Channels ?? [],
            Variables:       message.MetaData,
            Locale:          message.Locale,
            Email:           message.Email,
            PhoneNumber:     message.PhoneNumber,
            CorrelationId:   message.CorrelationId),
            context.CancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            _logger.LogWarning(
                "NotificationMessage TemplateCode={TemplateCode} had one or more failed channel dispatches.",
                message.TemplateCode);
        }
    }
}
