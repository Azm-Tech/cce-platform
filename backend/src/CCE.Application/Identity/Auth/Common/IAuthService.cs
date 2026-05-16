using CCE.Domain.Identity;

namespace CCE.Application.Identity.Auth.Common;

public sealed record RegisterResult(User? User, bool EmailTaken);

public interface IAuthService
{
    Task<AuthTokenDto?> LoginAsync(string email, string password, LocalAuthApi api, string? ip, string? userAgent, CancellationToken ct);

    Task<AuthTokenDto?> RefreshTokenAsync(string rawRefreshToken, LocalAuthApi api, string? ip, string? userAgent, CancellationToken ct);

    Task LogoutAsync(string rawRefreshToken, string? ip, CancellationToken ct);

    Task<RegisterResult> RegisterAsync(string firstName, string lastName, string email, string password, string? jobTitle, string? orgName, string? phone, CancellationToken ct);

    Task ForgotPasswordAsync(string email, CancellationToken ct);

    Task<string?> ResetPasswordAsync(string email, string encodedToken, string newPassword, string? ip, CancellationToken ct);
}
