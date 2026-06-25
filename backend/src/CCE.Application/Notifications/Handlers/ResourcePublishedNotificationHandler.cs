using CCE.Application.Content;
using CCE.Application.Notifications.Messages;
using CCE.Domain.Content.Events;
using CCE.Domain.Notifications;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CCE.Application.Notifications.Handlers;

public sealed class ResourcePublishedNotificationHandler
    : INotificationHandler<ResourcePublishedEvent>
{
    private readonly IResourceRepository _resourceRepo;
    private readonly INotificationMessageDispatcher _dispatcher;
    private readonly ILogger<ResourcePublishedNotificationHandler> _logger;

    public ResourcePublishedNotificationHandler(
        IResourceRepository resourceRepo,
        INotificationMessageDispatcher dispatcher,
        ILogger<ResourcePublishedNotificationHandler> logger)
    {
        _resourceRepo = resourceRepo;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task Handle(ResourcePublishedEvent notification, CancellationToken cancellationToken)
    {
        var resource = await _resourceRepo.FindAsync(notification.ResourceId, cancellationToken)
            .ConfigureAwait(false);

        if (resource is null)
        {
            _logger.LogWarning(
                "Resource {ResourceId} not found for notification.", notification.ResourceId);
            return;
        }

        await _dispatcher.DispatchAsync(new NotificationMessage(
            TemplateCode: "RESOURCE_PUBLISHED",
            RecipientUserId: resource.UploadedById,
            EventType: NotificationEventType.ResourcePublished,
            Channels: [NotificationChannel.InApp, NotificationChannel.Push],
            MetaData: new Dictionary<string, string>
            {
                ["TitleAr"] = resource.TitleAr,
                ["TitleEn"] = resource.TitleEn,
            },
            Locale: "en"), cancellationToken).ConfigureAwait(false);
    }
}
