using MassTransit;

namespace CCE.Infrastructure.Notifications.Messaging.Consumers;

public sealed class ContentFlaggedNotificationConsumerDefinition
    : ConsumerDefinition<ContentFlaggedNotificationConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ContentFlaggedNotificationConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        // Short retries — a transient SignalR/backplane hiccup shouldn't drop the moderator alert.
        endpointConfigurator.UseMessageRetry(r => r.Intervals(1000, 5000));
    }
}
