using CCE.Domain.Notifications;

namespace CCE.Application.Notifications;

public interface IUserDeviceTokenRepository
{
    Task<System.Collections.Generic.IReadOnlyList<UserDeviceToken>> GetActiveByUserIdAsync(
        System.Guid userId, CancellationToken cancellationToken);

    Task<UserDeviceToken?> GetByUserAndDeviceAsync(
        System.Guid userId, string deviceId, CancellationToken cancellationToken);

    Task AddAsync(UserDeviceToken token, CancellationToken cancellationToken);

    /// <summary>Deactivates tokens matching the given FCM token values after FCM rejects them.</summary>
    Task DeactivateByTokensAsync(
        System.Collections.Generic.IReadOnlyList<string> fcmTokens, CancellationToken cancellationToken);
}
