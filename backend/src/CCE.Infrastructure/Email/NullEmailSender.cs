using CCE.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CCE.Infrastructure.Email;

/// <summary>
/// Sub-11d — fallback transport. Logs the recipient + subject at
/// Information level and discards the message. Selected when
/// <c>Email:Provider=null</c> (or unset). Useful for tests + envs
/// without SMTP.
///
/// Body content is NEVER logged — implementations of <see cref="IEmailSender"/>
/// promise not to leak sensitive content like one-time passwords.
/// </summary>
public sealed class NullEmailSender : IEmailSender
{
    private readonly ILogger<NullEmailSender> _logger;

    public NullEmailSender(ILogger<NullEmailSender> logger) => _logger = logger;

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[NullEmailSender] Would have sent email to {To} with subject {Subject} (body suppressed)",
            to, subject);
        return Task.CompletedTask;
    }
}
