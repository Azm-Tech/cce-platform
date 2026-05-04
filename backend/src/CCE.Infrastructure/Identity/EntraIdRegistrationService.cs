using System.Security.Cryptography;
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
    private readonly ILogger<EntraIdRegistrationService> _logger;

    public EntraIdRegistrationService(
        EntraIdGraphClientFactory graphFactory,
        CceDbContext db,
        ILogger<EntraIdRegistrationService> logger)
    {
        _graphFactory = graphFactory;
        _db = db;
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

        return new RegistrationResult(
            EntraIdObjectId: cceUser.EntraIdObjectId!.Value,
            UserPrincipalName: created.UserPrincipalName!,
            DisplayName: created.DisplayName!,
            TemporaryPassword: tempPassword);
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
