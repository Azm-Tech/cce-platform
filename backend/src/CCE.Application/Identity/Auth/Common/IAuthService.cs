using CCE.Domain.Identity;

namespace CCE.Application.Identity.Auth.Common;

public sealed record RegisterResult(User? User, bool EmailTaken);

public sealed record AdminCreateResult(User? User, bool EmailTaken, bool Failed, bool PasswordResetSent);

/// <summary>Why a sign-in attempt failed, so the API can return a precise message.</summary>
public enum LoginFailureReason
{
    None = 0,
    InvalidCredentials = 1,
    Deactivated = 2,
    ContactNotVerified = 3,
}

/// <summary>Outcome of a sign-in attempt. <see cref="Token"/> is non-null only when <see cref="Failure"/> is None.</summary>
public sealed record LoginResult(AuthTokenDto? Token, LoginFailureReason Failure)
{
    public static LoginResult Success(AuthTokenDto token) => new(token, LoginFailureReason.None);
    public static readonly LoginResult InvalidCredentials = new(null, LoginFailureReason.InvalidCredentials);
    public static readonly LoginResult Deactivated = new(null, LoginFailureReason.Deactivated);
    public static readonly LoginResult ContactNotVerified = new(null, LoginFailureReason.ContactNotVerified);
}

public interface IAuthService
{
    Task<LoginResult> LoginAsync(string email, string password, LocalAuthApi api, string? ip, string? userAgent, CancellationToken ct);

    Task<AuthTokenDto?> RefreshTokenAsync(string rawRefreshToken, LocalAuthApi api, string? ip, string? userAgent, CancellationToken ct);

    Task LogoutAsync(string rawRefreshToken, string? ip, CancellationToken ct);

    Task<RegisterResult> RegisterAsync(string firstName, string lastName, string email, string password, string? jobTitle, string? orgName, string? phone, System.Guid? countryCodeId, CancellationToken ct);

    Task<AdminCreateResult> AdminCreateUserAsync(string firstName, string lastName, string email, string phone, System.Guid? countryId, System.Guid? countryCodeId, string role, CancellationToken ct);

    Task ForgotPasswordAsync(string email, CancellationToken ct);

    Task<string?> ResetPasswordAsync(string email, string encodedToken, string newPassword, string? ip, CancellationToken ct);

    Task<LoginResult> AdLoginAsync(string username, string password, string? ip, string? userAgent, CancellationToken ct);
}
