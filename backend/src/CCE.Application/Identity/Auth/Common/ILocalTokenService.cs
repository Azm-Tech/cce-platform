using CCE.Domain.Identity;

namespace CCE.Application.Identity.Auth.Common;

public interface ILocalTokenService
{
    Task<TokenIssueResult> IssueAsync(User user, LocalAuthApi api, CancellationToken ct);

    string HashRefreshToken(string refreshToken);
}
