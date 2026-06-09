using MassTransit;

namespace CCE.Infrastructure.Notifications.Messaging.Consumers;

public sealed class NotificationConsumerDefinition : ConsumerDefinition<NotificationConsumer>
{
    public NotificationConsumerDefinition()
    {
        ConcurrentMessageLimit = 10;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<NotificationConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Intervals(500, 2000, 5000));
    }
}
