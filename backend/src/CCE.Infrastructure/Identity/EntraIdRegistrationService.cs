using System.Net;
using System.Security.Cryptography;
using CCE.Application.Common.Interfaces;
using CCE.Domain.Identity;
using CCE.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;

namespace CCE.Infrastructure.Identity;

/// <summary>
/// Sub-11 Phase 01 — creates Entra ID users via Microsoft Graph
/// (User.ReadWrite.All app-only permission) and persists a stub CCE-side
/// <see cref="CCE.Domain.Identity.User"/> row linked by <c>EntraIdObjectId</c>.
/// Thin wrapper around <see cref="EntraIdGraphClientFactory"/> to keep the
/// service unit-testable against WireMock.
/// </summary>
public sealed class EntraIdRegistrationService
{
    private readonly EntraIdGraphClientFactory _graphFactory;
    private readonly CceDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EntraIdRegistrationService> _logger;

    public EntraIdRegistrationService(
        EntraIdGraphClientFactory graphFactory,
        CceDbContext db,
        IEmailSender emailSender,
        ILogger<EntraIdRegistrationService> logger)
    {
        _graphFactory = graphFactory;
        _db = db;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<RegistrationResult> CreateUserAsync(RegistrationRequest dto, CancellationToken ct)
    {
        var graph = _graphFactory.Create();
        var tempPassword = GenerateTempPassword();
        var newUser = new Microsoft.Graph.Models.User
        {
            DisplayName = $"{dto.GivenName} {dto.Surname}",
            GivenName = dto.GivenName,
            Surname = dto.Surname,
            Mail = dto.Email,
            UserPrincipalName = dto.Email,
            MailNickname = dto.MailNickname,
            AccountEnabled = true,
            UsageLocation = "SA",
            PasswordProfile = new PasswordProfile
            {
                Password = tempPassword,
                ForceChangePasswordNextSignIn = true,
            },
        };

        Microsoft.Graph.Models.User created;
        try
        {
            created = await graph.Users.PostAsync(newUser, cancellationToken: ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Microsoft Graph returned null user.");
        }
        catch (ODataError ex)
        {
            // Graph error mapping. UPN conflict shows up as 400 Request_BadRequest with
            // a "userPrincipalName already exists" message; 403 is Authorization_RequestDenied.
            var code = ex.Error?.Code ?? string.Empty;
            var msg = ex.Error?.Message ?? string.Empty;
            if (code == "Authorization_RequestDenied")
            {
                _logger.LogError(ex, "Graph rejected user-create with Authorization_RequestDenied; check User.ReadWrite.All consent");
                throw new EntraIdRegistrationAuthorizationException();
            }
            if (code == "Request_BadRequest"
                && msg.Contains("userPrincipalName", StringComparison.OrdinalIgnoreCase))
            {
                throw new EntraIdRegistrationConflictException(dto.Email);
            }
            _logger.LogError(ex, "Graph user-create failed with code {Code}", code);
            throw;
        }

        // CCE-side persist. Failure here surfaces a 500 to the caller; the operator
        // runbook in entra-id-troubleshooting.md (Phase 04) covers orphan recovery.
        var cceUser = CCE.Domain.Identity.User.CreateStubFromEntraId(
            objectId: Guid.Parse(created.Id!),
            email: created.UserPrincipalName!,
            displayName: created.DisplayName!);
        _db.Users.Add(cceUser);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Sub-11d — send welcome email with one-time password. We swallow
        // SMTP failures (logged at Error) rather than rolling back the
        // user-create: the user exists in Entra ID + CCE.DB; the operator
        // can rotate the password via Entra ID portal if the email never
        // landed. CA1031 suppressed because the catch is intentional —
        // SMTP transports throw a wide variety of types (SocketException,
        // SmtpCommandException, IOException, AuthenticationException, ...).
#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            var subject = "Welcome to CCE — your account is ready";
            var body = BuildWelcomeEmailHtml(dto, tempPassword);
            await _emailSender.SendAsync(created.UserPrincipalName!, subject, body, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send welcome email to {Upn}. User-create succeeded; operator must communicate password out-of-band.",
                created.UserPrincipalName);
        }
#pragma warning restore CA1031

        return new RegistrationResult(
            EntraIdObjectId: cceUser.EntraIdObjectId!.Value,
            UserPrincipalName: created.UserPrincipalName!,
            DisplayName: created.DisplayName!,
            TemporaryPassword: tempPassword);
    }

    private static string BuildWelcomeEmailHtml(RegistrationRequest dto, string tempPassword)
    {
        // HTML-encode all user-supplied + secret content. tempPassword is
        // base64url + "Aa1!" but encode anyway as defense-in-depth.
        var givenName = WebUtility.HtmlEncode(dto.GivenName);
        var encodedPassword = WebUtility.HtmlEncode(tempPassword);
        return $$"""
            <html>
              <body style="font-family: sans-serif; color: #333;">
                <h1>Welcome to CCE Knowledge Center, {{givenName}}!</h1>
                <p>Your CCE account has been created. Your one-time password is:</p>
                <p style="font-family: monospace; font-size: 1.2em; padding: 0.5em; background: #f5f5f5; display: inline-block;">
                  {{encodedPassword}}
                </p>
                <p>You'll be prompted to change this password on first sign-in.</p>
                <p>For security, this email is the only place this password appears. If you didn't request this account, contact your CCE administrator immediately.</p>
                <hr style="margin: 2em 0;" />
                <p style="font-size: 0.9em; color: #888;">CCE Knowledge Center — automated message</p>
              </body>
            </html>
            """;
    }

    private static string GenerateTempPassword()
    {
        // 16 bytes of cryptographically-strong randomness, base64url-encoded,
        // then appended with "Aa1!" to satisfy Entra ID default complexity
        // (mixed case + digit + symbol).
        Span<byte> buf = stackalloc byte[16];
        RandomNumberGenerator.Fill(buf);
        var raw = Convert.ToBase64String(buf)
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .TrimEnd('=');
        return raw[..Math.Min(16, raw.Length)] + "Aa1!";
    }
}
