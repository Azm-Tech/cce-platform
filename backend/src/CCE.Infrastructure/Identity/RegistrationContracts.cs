namespace CCE.Infrastructure.Identity;

/// <summary>
/// Inbound DTO for <c>EntraIdRegistrationService.CreateUserAsync</c>.
/// Mirrors the Microsoft Graph User object's create-user-required fields.
/// <c>MailNickname</c> is the pre-@ part of the userPrincipalName, e.g. "alice.smith".
/// </summary>
public sealed record RegistrationRequest(
    string GivenName,
    string Surname,
    string Email,
    string MailNickname);

/// <summary>
/// Result of a successful Entra ID user create + CCE-side persist.
/// Phase 01 returns the temp password to the caller because there's no
/// email-sender abstraction yet — the calling admin communicates it
/// out-of-band. Sub-11d will replace this with an email send.
/// <c>TemporaryPassword</c> is one-time; caller must not log it.
/// </summary>
public sealed record RegistrationResult(
    System.Guid EntraIdObjectId,
    string UserPrincipalName,
    string DisplayName,
    string TemporaryPassword);

/// <summary>
/// Thrown when Microsoft Graph returns a 409 (UPN already taken).
/// </summary>
public sealed class EntraIdRegistrationConflictException : System.Exception
{
    public EntraIdRegistrationConflictException() { }

    public EntraIdRegistrationConflictException(string upn)
        : base($"User principal name '{upn}' is already registered in Entra ID.") { }

    public EntraIdRegistrationConflictException(string message, System.Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Thrown when Microsoft Graph returns a 403 (insufficient privileges) —
/// indicates the app registration is missing User.ReadWrite.All or that
/// admin consent was never granted.
/// </summary>
public sealed class EntraIdRegistrationAuthorizationException : System.Exception
{
    public EntraIdRegistrationAuthorizationException()
        : base("CCE app registration lacks Graph permission to create users (User.ReadWrite.All not granted).") { }

    public EntraIdRegistrationAuthorizationException(string message)
        : base(message) { }

    public EntraIdRegistrationAuthorizationException(string message, System.Exception innerException)
        : base(message, innerException) { }
}
