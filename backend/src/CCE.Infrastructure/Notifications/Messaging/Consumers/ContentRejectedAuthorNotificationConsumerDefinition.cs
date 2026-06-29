using MassTransit;

namespace CCE.Infrastructure.Notifications.Messaging.Consumers;

public sealed class ContentRejectedAuthorNotificationConsumerDefinition
    : ConsumerDefinition<ContentRejectedAuthorNotificationConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ContentRejectedAuthorNotificationConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Intervals(1000, 5000));
    }
}
