using CCE.Application.Notifications;
using CCE.Domain.Notifications;
using CCE.Integration.Communication;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Notifications;

public sealed class SmsNotificationChannelSender : INotificationChannelHandler
{
    private readonly ICommunicationGatewayClient _client;
    private readonly ILogger<SmsNotificationChannelSender> _logger;

    public SmsNotificationChannelSender(
        ICommunicationGatewayClient client,
        ILogger<SmsNotificationChannelSender> logger)
    {
        _client = client;
        _logger = logger;
    }

    public NotificationChannel Channel => NotificationChannel.Sms;

    public bool ShouldSend(UserNotificationSettings? settings) => settings?.IsEnabled ?? true;

    public async Task<ChannelSendResult> SendAsync(
        RenderedNotification notification,
        CancellationToken cancellationToken)
    {
        var to = notification.PhoneNumber;
        if (string.IsNullOrWhiteSpace(to))
        {
            _logger.LogWarning(
                "Skipping SMS for template {TemplateCode}: no phone number.",
                notification.TemplateCode);
            return new ChannelSendResult(
                false, Error: "No recipient phone number available.");
        }

        try
        {
            var request = new SendSmsRequest(
                To: to,
                Message: notification.Body);

            var response = await _client.SendSmsAsync(request, cancellationToken)
                .ConfigureAwait(false);

            if (!"success".Equals(response.Status, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(
                    "Gateway SMS send failed for {To} template {TemplateCode}: {Error}",
                    to, notification.TemplateCode, response.Error);
                return new ChannelSendResult(
                    false, Error: $"Gateway SMS send failed: {response.Error}");
            }

            _logger.LogInformation(
                "Sent SMS via gateway to {To} template {TemplateCode} (id {Id})",
                to, notification.TemplateCode, response.Id);

            return new ChannelSendResult(true, ProviderMessageId: response.Id);
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "SMS channel HTTP failure for template {TemplateCode}",
                notification.TemplateCode);
            return new ChannelSendResult(false, Error: ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(
                ex,
                "SMS channel invalid operation for template {TemplateCode}",
                notification.TemplateCode);
            return new ChannelSendResult(false, Error: ex.Message);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken != cancellationToken)
        {
            _logger.LogError(
                ex,
                "SMS channel timeout for template {TemplateCode}",
                notification.TemplateCode);
            return new ChannelSendResult(false, Error: ex.Message);
        }
    }
}
