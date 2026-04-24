using CCE.Domain.Common;

namespace CCE.Infrastructure;

/// <summary>
/// Production <see cref="ISystemClock"/> implementation returning real UTC time.
/// Tests supply a fake that can be advanced explicitly.
/// </summary>
public sealed class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
