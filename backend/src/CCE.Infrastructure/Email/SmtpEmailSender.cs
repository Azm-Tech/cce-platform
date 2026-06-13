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

    public async Task SendAsync(string to, string subject, string htmlBody, string? templateId = null, CancellationToken ct = default)
    {
        var opts = _options.Value;
        using var message = new MimeMessage();
        message.From.Add(new MailboxAddress(opts.FromName, opts.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = WrapInLayout(htmlBody) }.ToMessageBody();

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

    private static string WrapInLayout(string body)
    {
        // If the body already starts with <!DOCTYPE or <html, it's already a full document.
        if (body.TrimStart().StartsWith("<!DOCTYPE", System.StringComparison.OrdinalIgnoreCase) ||
            body.TrimStart().StartsWith("<html", System.StringComparison.OrdinalIgnoreCase))
        {
            return body;
        }

        return $@"<!DOCTYPE html>
<html lang=""en"" dir=""ltr"">
<head>
    <meta charset=""UTF-8"" />
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
    <title></title>
    <style type=""text/css"">
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{ background-color: #f4f6f9; font-family: 'Segoe UI', Tahoma, Arial, sans-serif; font-size: 16px; line-height: 1.6; color: #333; }}
        .email-wrapper {{ max-width: 600px; margin: 0 auto; padding: 20px 10px; }}
        .email-header {{ background: linear-gradient(135deg, #1a73e8, #0d47a1); padding: 30px 24px; border-radius: 8px 8px 0 0; text-align: center; }}
        .email-header h1 {{ color: #fff; font-size: 22px; font-weight: 600; margin: 0; }}
        .email-body {{ background-color: #fff; padding: 32px 24px; border-left: 1px solid #e0e0e0; border-right: 1px solid #e0e0e0; }}
        .email-footer {{ background-color: #eef1f5; padding: 20px 24px; border-radius: 0 0 8px 8px; text-align: center; font-size: 13px; color: #777; border: 1px solid #e0e0e0; border-top: none; }}
        .email-footer a {{ color: #1a73e8; text-decoration: none; }}
        .btn {{ display: inline-block; padding: 12px 28px; background-color: #1a73e8; color: #fff !important; text-decoration: none; border-radius: 6px; font-size: 15px; font-weight: 500; margin: 16px 0; }}
        .btn:hover {{ background-color: #1557b0; }}
        h2 {{ font-size: 20px; margin: 20px 0 10px; color: #222; }}
        p {{ margin: 10px 0; }}
    </style>
</head>
<body>
    <div class=""email-wrapper"">
        <div class=""email-header"">
            <h1>CCE Knowledge Center</h1>
        </div>
        <div class=""email-body"">
            {body}
        </div>
        <div class=""email-footer"">
            <p>&copy; {System.DateTime.Now.Year} CCE Knowledge Center. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }
}
