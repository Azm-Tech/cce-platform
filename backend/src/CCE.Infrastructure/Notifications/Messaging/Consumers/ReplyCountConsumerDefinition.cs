using MassTransit;

namespace CCE.Infrastructure.Notifications.Messaging.Consumers;

public sealed class ReplyCountConsumerDefinition : ConsumerDefinition<ReplyCountConsumer>
{
    public ReplyCountConsumerDefinition()
    {
        ConcurrentMessageLimit = 50;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ReplyCountConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Intervals(200, 500, 1000));
    }
}
