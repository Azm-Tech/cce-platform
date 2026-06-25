using System.Collections.Generic;
using CCE.Application.Notifications;
using CCE.Domain.Notifications;
using CCE.Infrastructure.Firebase;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Notifications;

public sealed class PushNotificationChannelSender : INotificationChannelHandler
{
    // FCM error codes meaning the token is permanently invalid.
    private static readonly HashSet<string> _staleTokenCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "UNREGISTERED",
        "INVALID_ARGUMENT",
        "SENDER_ID_MISMATCH"
    };

    private readonly IUserDeviceTokenRepository _tokenRepo;
    private readonly IFirebaseMessagingService _firebase;
    private readonly ILogger<PushNotificationChannelSender> _logger;

    public PushNotificationChannelSender(
        IUserDeviceTokenRepository tokenRepo,
        IFirebaseMessagingService firebase,
        ILogger<PushNotificationChannelSender> logger)
    {
        _tokenRepo = tokenRepo;
        _firebase = firebase;
        _logger = logger;
    }

    public NotificationChannel Channel => NotificationChannel.Push;

    public bool ShouldSend(UserNotificationSettings? settings) => settings?.IsEnabled ?? true;

    public async Task<ChannelSendResult> SendAsync(
        RenderedNotification notification,
        CancellationToken cancellationToken)
    {
        if (notification.RecipientUserId is null)
            return new ChannelSendResult(false, Error: "Push requires a recipient user ID.");

        var deviceTokens = await _tokenRepo
            .GetActiveByUserIdAsync(notification.RecipientUserId.Value, cancellationToken)
            .ConfigureAwait(false);

        if (deviceTokens.Count == 0)
        {
            _logger.LogDebug(
                "No active device tokens for user {UserId}; skipping push for {TemplateCode}.",
                notification.RecipientUserId, notification.TemplateCode);
            return new ChannelSendResult(true, ProviderMessageId: "no-devices");
        }

        var rawTokens = new List<string>(deviceTokens.Count);
        foreach (var dt in deviceTokens)
            rawTokens.Add(dt.Token);

        var data = new Dictionary<string, string>
        {
            ["templateCode"] = notification.TemplateCode,
            ["locale"] = notification.Locale
        };

        if (notification.MetaData is not null)
        {
            foreach (var kv in notification.MetaData)
                data[kv.Key] = kv.Value;
        }

        var message = new MulticastMessage
        {
            Tokens = rawTokens,
            Notification = new Notification
            {
                Title = notification.Subject,
                Body = notification.Body
            },
            Data = data,
            Apns = new ApnsConfig { Aps = new Aps { Sound = "default" } },
            Android = new AndroidConfig { Priority = Priority.High }
        };

        var batchResponse = await _firebase
            .SendMulticastAsync(message, cancellationToken)
            .ConfigureAwait(false);

        var staleTokens = new List<string>();
        for (var i = 0; i < batchResponse.Responses.Count; i++)
        {
            var r = batchResponse.Responses[i];
            if (!r.IsSuccess && r.Exception?.MessagingErrorCode is { } code
                && _staleTokenCodes.Contains(code.ToString()))
            {
                staleTokens.Add(rawTokens[i]);
            }
        }

        if (staleTokens.Count > 0)
        {
            _logger.LogInformation(
                "Deactivating {Count} stale FCM tokens for user {UserId}.",
                staleTokens.Count, notification.RecipientUserId);
            await _tokenRepo
                .DeactivateByTokensAsync(staleTokens, cancellationToken)
                .ConfigureAwait(false);
        }

        var success = batchResponse.SuccessCount > 0 || deviceTokens.Count == 0;
        return new ChannelSendResult(
            success,
            Error: success ? null : $"All {batchResponse.FailureCount} FCM sends failed.");
    }
}
