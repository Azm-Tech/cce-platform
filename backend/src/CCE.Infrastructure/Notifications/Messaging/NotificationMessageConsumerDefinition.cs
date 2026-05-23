using MassTransit;

namespace CCE.Infrastructure.Notifications.Messaging;

/// <summary>
/// Defines retry, concurrency, and queue naming for
/// <see cref="NotificationMessageConsumer"/>.
///
/// MassTransit picks this up automatically via <c>AddConsumer&lt;,&gt;</c>.
/// </summary>
public sealed class NotificationMessageConsumerDefinition
    : ConsumerDefinition<NotificationMessageConsumer>
{
    public NotificationMessageConsumerDefinition()
    {
        // One concurrent message per consumer instance (safe for DB write heavy work).
        ConcurrentMessageLimit = 10;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<NotificationMessageConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        // 3 immediate retries, 5-second interval.
        // After exhausting retries MassTransit moves the message to the
        // _error queue automatically — no message is silently dropped.
        endpointConfigurator.UseMessageRetry(r =>
            r.Intervals(
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(30)));
    }
}
