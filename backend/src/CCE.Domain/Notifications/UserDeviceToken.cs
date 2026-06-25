using CCE.Domain.Common;

namespace CCE.Domain.Notifications;

/// <summary>
/// FCM registration token for a physical device.
/// One row per (UserId, DeviceId). DeviceId is a stable client-generated UUID; Token rotates.
/// NOT audited — high-cardinality, managed by device lifecycle.
/// </summary>
public sealed class UserDeviceToken : Entity<System.Guid>
{
    private UserDeviceToken(
        System.Guid id,
        System.Guid userId,
        string deviceId,
        string token,
        string platform,
        System.DateTimeOffset registeredOn) : base(id)
    {
        UserId = userId;
        DeviceId = deviceId;
        Token = token;
        Platform = platform;
        RegisteredOn = registeredOn;
        LastSeenOn = registeredOn;
        IsActive = true;
    }

    public System.Guid UserId { get; private set; }
    /// <summary>Stable UUID the client generates on first launch. Never rotates.</summary>
    public string DeviceId { get; private set; }
    /// <summary>FCM registration token. Rotates; updated via Refresh().</summary>
    public string Token { get; private set; }
    /// <summary>"ios" | "android" | "web"</summary>
    public string Platform { get; private set; }
    public System.DateTimeOffset RegisteredOn { get; private set; }
    public System.DateTimeOffset LastSeenOn { get; private set; }
    public bool IsActive { get; private set; }

    public static UserDeviceToken Register(
        System.Guid userId,
        string deviceId,
        string token,
        string platform,
        ISystemClock clock)
    {
        if (userId == System.Guid.Empty) throw new DomainException("UserId is required.");
        if (string.IsNullOrWhiteSpace(deviceId)) throw new DomainException("DeviceId is required.");
        if (string.IsNullOrWhiteSpace(token)) throw new DomainException("Token is required.");
        if (platform is not ("ios" or "android" or "web"))
            throw new DomainException("Platform must be 'ios', 'android', or 'web'.");
        return new UserDeviceToken(System.Guid.NewGuid(), userId, deviceId, token, platform, clock.UtcNow);
    }

    /// <summary>Called when the client reports a refreshed FCM token for an existing device.</summary>
    public void Refresh(string newToken, ISystemClock clock)
    {
        if (string.IsNullOrWhiteSpace(newToken)) throw new DomainException("Token is required.");
        Token = newToken;
        LastSeenOn = clock.UtcNow;
        IsActive = true;
    }

    /// <summary>Called when FCM reports the token is no longer valid.</summary>
    public void Deactivate() => IsActive = false;
}
