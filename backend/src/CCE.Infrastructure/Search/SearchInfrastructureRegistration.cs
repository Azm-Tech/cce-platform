using Microsoft.Extensions.DependencyInjection;

namespace CCE.Infrastructure.Search;

public static class SearchInfrastructureRegistration
{
    public static IServiceCollection AddCceMeilisearchIndexer(this IServiceCollection services)
    {
        services.AddHostedService<MeilisearchIndexer>();

        // Notification handlers live in CCE.Infrastructure, outside the MediatR assembly scan
        // scoped to CCE.Application. Register them explicitly so they are discovered.
        services.AddTransient<MediatR.INotificationHandler<CCE.Domain.Content.Events.NewsPublishedEvent>,   NewsPublishedIndexHandler>();
        services.AddTransient<MediatR.INotificationHandler<CCE.Domain.Content.Events.ResourcePublishedEvent>, ResourcePublishedIndexHandler>();
        services.AddTransient<MediatR.INotificationHandler<CCE.Domain.Content.Events.EventScheduledEvent>,  EventScheduledIndexHandler>();

        // Community search indexers — keep post and reply indexes up to date as content is published.
        // SEARCH-INDEX-NOTE: When reply soft-delete is implemented, add a handler calling DeleteAsync(CommunityReplies, replyId, ct).
        // SEARCH-INDEX-NOTE: When reply/post edit is implemented, re-upsert the updated document after commit.
        services.AddTransient<MediatR.INotificationHandler<CCE.Domain.Community.Events.PostCreatedEvent>,  PostCreatedSearchIndexHandler>();
        services.AddTransient<MediatR.INotificationHandler<CCE.Domain.Community.Events.ReplyCreatedEvent>, ReplyCreatedSearchIndexHandler>();

        return services;
    }
}
