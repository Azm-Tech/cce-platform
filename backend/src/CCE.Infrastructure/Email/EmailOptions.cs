namespace CCE.Infrastructure.Email;

/// <summary>
/// Sub-11d — strongly-typed accessor for the <c>Email:</c> appsettings
/// section. Drives <c>SmtpEmailSender</c> + <c>NullEmailSender</c>
/// selection.
///
/// Provider semantics:
/// <list type="bullet">
///   <item><c>"smtp"</c> — uses SMTP. <c>Host</c>+<c>Port</c> required;
///   <c>Username</c>+<c>Password</c> optional (omit for unauthenticated
///   relays like the local MailDev container).</item>
///   <item><c>"null"</c> (or empty / unset) — uses
///   <see cref="NullEmailSender"/>. Logs the message at Information
///   level and discards. Default for tests + envs without SMTP.</item>
/// </list>
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>One of: "smtp" or "null". Default: "null".</summary>
    public string Provider { get; init; } = "null";

    /// <summary>SMTP server hostname. Required when Provider=="smtp".</summary>
    public string Host { get; init; } = string.Empty;

    /// <summary>SMTP server port. Common values: 25, 465 (SSL), 587 (STARTTLS), 1025 (MailDev).</summary>
    public int Port { get; init; } = 25;

    /// <summary>From-address for outbound mail (e.g. "no-reply@cce.local").</summary>
    public string FromAddress { get; init; } = "no-reply@cce.local";

    /// <summary>Display name for the From: header (e.g. "CCE Knowledge Center").</summary>
    public string FromName { get; init; } = "CCE Knowledge Center";

    /// <summary>SMTP auth username (optional — empty for MailDev).</summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>SMTP auth password (optional — empty for MailDev).</summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Whether to negotiate STARTTLS / SSL. Default: false (plain SMTP).
    /// Set to true for prod ports 465 / 587.
    /// </summary>
    public bool EnableSsl { get; init; }
}
