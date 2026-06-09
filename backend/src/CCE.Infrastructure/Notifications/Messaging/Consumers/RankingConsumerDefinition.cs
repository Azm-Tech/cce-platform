using MassTransit;

namespace CCE.Infrastructure.Notifications.Messaging.Consumers;

public sealed class RankingConsumerDefinition : ConsumerDefinition<RankingConsumer>
{
    public RankingConsumerDefinition()
    {
        // Serialize to prevent concurrent leaderboard corruption.
        ConcurrentMessageLimit = 1;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<RankingConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r => r.Intervals(1000, 3000, 5000));
    }
}
