using CCE.Application.Common.Interfaces;
using CCE.Application.Notifications;
using CCE.Domain.Notifications;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Notifications;

public sealed class EmailNotificationChannelSender : INotificationChannelHandler
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailNotificationChannelSender> _logger;

    public EmailNotificationChannelSender(
        IEmailSender emailSender,
        ILogger<EmailNotificationChannelSender> logger)
    {
        _emailSender = emailSender;
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
            await _emailSender.SendAsync(
                to,
                notification.Subject,
                notification.Body,
                notification.TemplateCode,
                cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Sent email via SMTP to {To} template {TemplateCode}",
                to, notification.TemplateCode);

            return new ChannelSendResult(true);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(
                ex,
                "SMTP email send failed for {To} template {TemplateCode}",
                to, notification.TemplateCode);
            return new ChannelSendResult(false, Error: ex.Message);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken != cancellationToken)
        {
            _logger.LogError(
                ex,
                "SMTP email send timed out for {To} template {TemplateCode}",
                to, notification.TemplateCode);
            return new ChannelSendResult(false, Error: ex.Message);
        }
        catch (MailKit.Net.Smtp.SmtpCommandException ex)
        {
            _logger.LogError(
                ex,
                "SMTP command failed for {To} template {TemplateCode}",
                to, notification.TemplateCode);
            return new ChannelSendResult(false, Error: ex.Message);
        }
        catch (MailKit.Net.Smtp.SmtpProtocolException ex)
        {
            _logger.LogError(
                ex,
                "SMTP protocol error for {To} template {TemplateCode}",
                to, notification.TemplateCode);
            return new ChannelSendResult(false, Error: ex.Message);
        }
    }
}
