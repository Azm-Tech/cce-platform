using System.Text.Json;
using CCE.Application.Common.Interfaces;
using CCE.Application.Common.Pagination;
using CCE.Application.Notifications;
using CCE.Domain.Common;
using CCE.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Notifications;

public sealed class NotificationGateway : INotificationGateway
{
    private readonly ICceDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly INotificationTemplateRepository _templates;
    private readonly IUserNotificationSettingsRepository _settings;
    private readonly INotificationLogRepository _logs;
    private readonly INotificationTemplateRenderer _renderer;
    private readonly IEnumerable<INotificationChannelHandler> _channelHandlers;
    private readonly ISignalRNotificationPublisher? _signalR;
    private readonly ILogger<NotificationGateway> _logger;

    public NotificationGateway(
        ICceDbContext db,
        ICurrentUserAccessor currentUser,
        INotificationTemplateRepository templates,
        IUserNotificationSettingsRepository settings,
        INotificationLogRepository logs,
        INotificationTemplateRenderer renderer,
        IEnumerable<INotificationChannelHandler> channelHandlers,
        ILogger<NotificationGateway> logger,
        ISignalRNotificationPublisher? signalR = null)
    {
        _db = db;
        _currentUser = currentUser;
        _templates = templates;
        _settings = settings;
        _logs = logs;
        _renderer = renderer;
        _channelHandlers = channelHandlers;
        _logger = logger;
        _signalR = signalR;
    }

    public async Task<NotificationDispatchResult> SendAsync(
        NotificationDispatchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.TemplateCode))
            throw new DomainException("TemplateCode is required.");

        var requestedChannels = request.Channels?.ToList() ?? [];

        // Resolve recipient data
        string? email = request.Email;
        string? phone = request.PhoneNumber;
        string locale = request.Locale;

        if (request.RecipientUserId is { } userId)
        {
            var user = (await _db.Users
                .Where(u => u.Id == userId)
                .Select(u => new { u.Email, u.PhoneNumber })
                .ToListAsyncEither(cancellationToken)
                .ConfigureAwait(false))
                .FirstOrDefault();

            if (user is not null)
            {
                email ??= user.Email;
                phone ??= user.PhoneNumber;
            }
        }

        var correlationId = request.CorrelationId ?? _currentUser.GetCorrelationId().ToString("N");
        var results = new List<NotificationChannelDispatchResult>();
        var inAppUserNotifications = new List<UserNotification>();

        var templates = await _templates
            .ListActiveByCodeAsync(request.TemplateCode, cancellationToken)
            .ConfigureAwait(false);

        var templateByChannel = templates.ToDictionary(t => t.Channel);
        var channels = requestedChannels.Count == 0
            ? templateByChannel.Keys.ToList()
            : requestedChannels;

        if (channels.Count == 0)
        {
            _logger.LogWarning(
                "No active notification templates found for code {TemplateCode}.",
                request.TemplateCode);
            return new NotificationDispatchResult(
                request.TemplateCode,
                request.RecipientUserId,
                []);
        }

        // Load user settings if applicable
        Dictionary<(NotificationChannel, string?), UserNotificationSettings>? settingsMap = null;
        if (request.RecipientUserId is { } settingsUserId)
        {
            var settings = await _settings
                .ListForUserAndChannelsAsync(settingsUserId, channels, cancellationToken)
                .ConfigureAwait(false);

            settingsMap = settings.ToDictionary(
                s => (s.Channel, (string?)s.EventCode),
                s => s);
        }

        foreach (var channel in channels)
        {
            var result = await DispatchChannelAsync(
                request,
                channel,
                email,
                phone,
                locale,
                templateByChannel,
                settingsMap,
                correlationId,
                inAppUserNotifications,
                cancellationToken).ConfigureAwait(false);

            results.Add(result);
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // SignalR push after persistence
        if (_signalR is not null && inAppUserNotifications.Count > 0)
        {
            foreach (var notif in inAppUserNotifications)
            {
                await _signalR.PublishAsync(notif, cancellationToken).ConfigureAwait(false);
            }
        }

        return new NotificationDispatchResult(
            request.TemplateCode,
            request.RecipientUserId,
            results);
    }

    private async Task<NotificationChannelDispatchResult> DispatchChannelAsync(
        NotificationDispatchRequest request,
        NotificationChannel channel,
        string? email,
        string? phone,
        string locale,
        Dictionary<NotificationChannel, NotificationTemplate> templateByChannel,
        Dictionary<(NotificationChannel, string?), UserNotificationSettings>? settingsMap,
        string correlationId,
        List<UserNotification> inAppUserNotifications,
        CancellationToken cancellationToken)
    {
        // Skip in-app/SMS for anonymous users
        if (request.RecipientUserId is null && channel is NotificationChannel.InApp)
        {
            return new NotificationChannelDispatchResult(
                channel,
                NotificationDeliveryStatus.Skipped,
                Error: "In-app notifications require a recipient user ID.");
        }

        UserNotificationSettings? channelSettings = null;
        if (!request.BypassSettings && settingsMap is not null)
        {
            var eventKey = (channel, (string?)request.TemplateCode);
            var defaultKey = (channel, (string?)null);

            if (!settingsMap.TryGetValue(eventKey, out channelSettings))
            {
                settingsMap.TryGetValue(defaultKey, out channelSettings);
            }
        }

        // Resolve template
        if (!templateByChannel.TryGetValue(channel, out var template))
        {
            var log = NotificationLog.Create(
                request.RecipientUserId,
                request.TemplateCode,
                null,
                channel,
                correlationId: correlationId);
            log.MarkSkipped($"No active template found for channel {channel}.");
            await _logs.AddAsync(log, cancellationToken).ConfigureAwait(false);

            return new NotificationChannelDispatchResult(
                channel,
                NotificationDeliveryStatus.Skipped,
                NotificationLogId: log.Id,
                Error: $"No active template found for channel {channel}.");
        }

        // Render
        var variables = request.Variables ?? new Dictionary<string, string>();
        var (subjectAr, subjectEn, body) = _renderer.Render(template, variables, locale);
        var subject = locale == "ar" ? subjectAr : subjectEn;

        var rendered = new RenderedNotification(
            request.TemplateCode,
            request.RecipientUserId,
            template.Id,
            subject,
            subjectAr,
            subjectEn,
            body,
            channel,
            locale,
            email,
            phone);

        // Create pending log
        var payloadJson = SerializePayload(variables);
        var notificationLog = NotificationLog.Create(
            request.RecipientUserId,
            request.TemplateCode,
            template.Id,
            channel,
            payloadJson,
            correlationId);
        await _logs.AddAsync(notificationLog, cancellationToken).ConfigureAwait(false);

        // Dispatch
        var sender = _channelHandlers.FirstOrDefault(s => s.Channel == channel);
        if (sender is null)
        {
            notificationLog.MarkSkipped($"No sender registered for channel {channel}.");
            return new NotificationChannelDispatchResult(
                channel,
                NotificationDeliveryStatus.Skipped,
                NotificationLogId: notificationLog.Id,
                Error: $"No sender registered for channel {channel}.");
        }

        if (!sender.ShouldSend(channelSettings))
        {
            notificationLog.MarkSkipped("Channel disabled by user settings.");
            return new NotificationChannelDispatchResult(
                channel,
                NotificationDeliveryStatus.Skipped,
                NotificationLogId: notificationLog.Id,
                Error: "Channel disabled by user settings.");
        }

        var sendResult = await sender.SendAsync(rendered, cancellationToken).ConfigureAwait(false);

        if (sendResult.Success)
        {
            notificationLog.MarkSent(sendResult.ProviderMessageId);
        }
        else
        {
            notificationLog.MarkFailed(sendResult.Error ?? "Unknown error");
        }

        // Collect in-app notifications for batch persistence
        if (channel == NotificationChannel.InApp && sendResult.UserNotification is { } userNotification)
        {
            inAppUserNotifications.Add(userNotification);
        }

        return new NotificationChannelDispatchResult(
            channel,
            sendResult.Success ? NotificationDeliveryStatus.Sent : NotificationDeliveryStatus.Failed,
            NotificationLogId: notificationLog.Id,
            UserNotificationId: sendResult.UserNotificationId,
            ProviderMessageId: sendResult.ProviderMessageId,
            Error: sendResult.Error);
    }

    private static string? SerializePayload(IReadOnlyDictionary<string, string> variables)
    {
        try
        {
            return JsonSerializer.Serialize(variables);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
