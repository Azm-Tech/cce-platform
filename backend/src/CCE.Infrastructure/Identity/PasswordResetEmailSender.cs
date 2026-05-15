using System.Net;
using CCE.Application.Common.Interfaces;
using CCE.Application.Identity.Auth.Common;
using CCE.Domain.Identity;
using Microsoft.Extensions.Configuration;

namespace CCE.Infrastructure.Identity;

public sealed class PasswordResetEmailSender : IPasswordResetEmailSender
{
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;

    public PasswordResetEmailSender(IEmailSender emailSender, IConfiguration configuration)
    {
        _emailSender = emailSender;
        _configuration = configuration;
    }

    public async Task SendAsync(User user, string resetToken, CancellationToken ct)
    {
        var baseUrl = _configuration.GetValue<string>("Frontend:PasswordResetUrl")
            ?? "http://localhost:4200/reset-password";
        var separator = baseUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        var url = $"{baseUrl}{separator}email={Uri.EscapeDataString(user.Email ?? string.Empty)}&token={Uri.EscapeDataString(resetToken)}";
        var firstName = WebUtility.HtmlEncode(user.FirstName);
        var encodedUrl = WebUtility.HtmlEncode(url);
        var body = $$"""
            <html>
              <body style="font-family: sans-serif; color: #333;">
                <p>Hello {{firstName}},</p>
                <p>Use the link below to reset your CCE password.</p>
                <p><a href="{{encodedUrl}}">Reset password</a></p>
                <p>If you did not request a password reset, you can ignore this email.</p>
              </body>
            </html>
            """;

        await _emailSender.SendAsync(user.Email ?? string.Empty, "Reset your CCE password", body, ct)
            .ConfigureAwait(false);
    }
}
