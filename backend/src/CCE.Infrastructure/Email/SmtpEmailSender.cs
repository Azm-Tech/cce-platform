using CCE.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CCE.Infrastructure.Email;

/// <summary>
/// Sub-11d — MailKit-backed SMTP transport. Selected when
/// <c>Email:Provider=smtp</c>. Authentication is optional (skipped when
/// <c>Username</c> is empty — MailDev local-dev pattern).
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IOptions<EmailOptions> _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var opts = _options.Value;
        using var message = new MimeMessage();
        message.From.Add(new MailboxAddress(opts.FromName, opts.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        try
        {
            var secureSocketOptions = opts.EnableSsl
                ? SecureSocketOptions.StartTlsWhenAvailable
                : SecureSocketOptions.None;
            await client.ConnectAsync(opts.Host, opts.Port, secureSocketOptions, ct).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(opts.Username))
            {
                await client.AuthenticateAsync(opts.Username, opts.Password, ct).ConfigureAwait(false);
            }
            await client.SendAsync(message, ct).ConfigureAwait(false);
            await client.DisconnectAsync(quit: true, ct).ConfigureAwait(false);
            // Subject + recipient logged; body deliberately omitted (may carry secrets).
            _logger.LogInformation("Sent email to {To} with subject {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP send failed for {To} via {Host}:{Port}", to, opts.Host, opts.Port);
            throw;
        }
    }
}
