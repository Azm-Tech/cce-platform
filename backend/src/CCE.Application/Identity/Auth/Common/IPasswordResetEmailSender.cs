using CCE.Domain.Identity;

namespace CCE.Application.Identity.Auth.Common;

public interface IPasswordResetEmailSender
{
    Task SendAsync(User user, string resetToken, CancellationToken ct);
}
