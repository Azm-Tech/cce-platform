namespace CCE.Application.Common.Interfaces;

/// <summary>
/// Sub-11d — outbound email transport. Sends transactional messages
/// (welcome emails, password resets, expert-request approval notices)
/// to known external recipients.
///
/// Implementations:
/// <list type="bullet">
///   <item><c>SmtpEmailSender</c> — production. Talks to any SMTP server
///   (MailKit-backed). Configured via <c>EmailOptions</c> from the
///   <c>Email:</c> appsettings section.</item>
///   <item><c>NullEmailSender</c> — fallback. Logs the message at
///   Information level and discards. Used when <c>Email:Provider=null</c>
///   or when SMTP host configuration is absent.</item>
/// </list>
///
/// Callers MUST NOT log <see cref="SendAsync"/>'s body parameters — they
/// commonly contain sensitive content (one-time passwords, recovery
/// links). Implementations log only the recipient address + subject.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends a single email to one recipient. Throws on transport
    /// failure; callers handle by logging + rethrowing or surfacing a
    /// 5xx to the user-facing API.
    /// </summary>
    /// <param name="to">Recipient address. Must be a valid RFC-5322 address.</param>
    /// <param name="subject">Subject line. Plain text; no formatting.</param>
    /// <param name="htmlBody">HTML body. Sanitized HTML allowed.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
