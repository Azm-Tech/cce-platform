using MassTransit;

namespace CCE.Infrastructure.Notifications.Messaging.Consumers;

public sealed class FeedConsumerDefinition : ConsumerDefinition<FeedConsumer>
{
    public FeedConsumerDefinition()
    {
        ConcurrentMessageLimit = 20;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<FeedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Intervals(500, 2000, 5000));
    }
}
