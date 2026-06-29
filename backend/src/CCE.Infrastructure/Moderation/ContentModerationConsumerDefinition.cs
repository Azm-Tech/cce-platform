using MassTransit;

namespace CCE.Infrastructure.Moderation;

public sealed class ContentModerationConsumerDefinition : ConsumerDefinition<ContentModerationConsumer>
{
    public ContentModerationConsumerDefinition()
    {
        ConcurrentMessageLimit = 5;
    }

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<ContentModerationConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        // Back off when the AI provider rate-limits (Groq free tier ~30 req/min).
        endpointConfigurator.UseMessageRetry(r => r.Intervals(2000, 10_000, 30_000));
        endpointConfigurator.UseRateLimit(60, System.TimeSpan.FromMinutes(1));
    }
}
