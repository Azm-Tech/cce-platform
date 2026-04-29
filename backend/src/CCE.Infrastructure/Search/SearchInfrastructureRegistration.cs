using Microsoft.Extensions.DependencyInjection;

namespace CCE.Infrastructure.Search;

public static class SearchInfrastructureRegistration
{
    public static IServiceCollection AddCceMeilisearchIndexer(this IServiceCollection services)
    {
        services.AddHostedService<MeilisearchIndexer>();
        // Notification handlers live in CCE.Infrastructure, outside the MediatR assembly scan
        // scoped to CCE.Application. Register them explicitly so they are discovered.
        services.AddTransient<MediatR.INotificationHandler<CCE.Domain.Content.Events.NewsPublishedEvent>, NewsPublishedIndexHandler>();
        services.AddTransient<MediatR.INotificationHandler<CCE.Domain.Content.Events.ResourcePublishedEvent>, ResourcePublishedIndexHandler>();
        services.AddTransient<MediatR.INotificationHandler<CCE.Domain.Content.Events.EventScheduledEvent>, EventScheduledIndexHandler>();
        return services;
    }
}
