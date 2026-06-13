using MassTransit;

namespace CCE.Infrastructure.Notifications.Messaging.Consumers;

public sealed class ContentNotificationConsumerDefinition
    : ConsumerDefinition<ContentNotificationConsumer>
{
    public ContentNotificationConsumerDefinition()
    {
        ConcurrentMessageLimit = 5;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ContentNotificationConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Intervals(500, 2000, 5000));
    }
}
