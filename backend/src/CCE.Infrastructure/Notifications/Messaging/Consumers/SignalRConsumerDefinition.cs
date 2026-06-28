using MassTransit;

namespace CCE.Infrastructure.Notifications.Messaging.Consumers;

public sealed class SignalRConsumerDefinition : ConsumerDefinition<SignalRConsumer>
{
    public SignalRConsumerDefinition()
    {
        ConcurrentMessageLimit = 30;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<SignalRConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Intervals(200, 500, 1000));
    }
}
