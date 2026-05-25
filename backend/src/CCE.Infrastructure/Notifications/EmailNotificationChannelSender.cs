using CCE.Application.Notifications;
using CCE.Domain.Notifications;
using CCE.Infrastructure.Email;
using CCE.Integration.Communication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CCE.Infrastructure.Notifications;

public sealed class EmailNotificationChannelSender : INotificationChannelHandler
{
    private readonly ICommunicationGatewayClient _client;
    private readonly IOptions<EmailOptions> _options;
    private readonly ILogger<EmailNotificationChannelSender> _logger;

    public EmailNotificationChannelSender(
        ICommunicationGatewayClient client,
        IOptions<EmailOptions> options,
        ILogger<EmailNotificationChannelSender> logger)
    {
        _client = client;
        _options = options;
        _logger = logger;
    }

    public NotificationChannel Channel => NotificationChannel.Email;

    public bool ShouldSend(UserNotificationSettings? settings) => settings?.IsEnabled ?? true;

    public async Task<ChannelSendResult> SendAsync(
        RenderedNotification notification,
        CancellationToken cancellationToken)
    {
        var to = notification.Email;
        if (string.IsNullOrWhiteSpace(to))
        {
            _logger.LogWarning(
                "Skipping email for template {TemplateCode}: no recipient email.",
                notification.TemplateCode);
            return new ChannelSendResult(
                false, Error: "No recipient email address available.");
        }

        try
        {
            var request = new SendEmailRequest(
                To: to,
                From: _options.Value.FromAddress,
                Subject: notification.Subject,
                Html: notification.Body);

            var response = await _client.SendEmailAsync(request, cancellationToken)
                .ConfigureAwait(false);

            if (!"success".Equals(response.Status, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(
                    "Gateway email send failed for {To} template {TemplateCode}: {Error}",
                    to, notification.TemplateCode, response.Error);
                return new ChannelSendResult(
                    false, Error: $"Gateway email send failed: {response.Error}");
            }

            _logger.LogInformation(
                "Sent email via gateway to {To} template {TemplateCode} (id {Id})",
                to, notification.TemplateCode, response.Id);

            return new ChannelSendResult(true, ProviderMessageId: response.Id);
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Email channel HTTP failure for template {TemplateCode}",
                notification.TemplateCode);
            return new ChannelSendResult(false, Error: ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(
                ex,
                "Email channel invalid operation for template {TemplateCode}",
                notification.TemplateCode);
            return new ChannelSendResult(false, Error: ex.Message);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken != cancellationToken)
        {
            _logger.LogError(
                ex,
                "Email channel timeout for template {TemplateCode}",
                notification.TemplateCode);
            return new ChannelSendResult(false, Error: ex.Message);
        }
    }
}
