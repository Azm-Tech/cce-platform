namespace CCE.Domain.Common;

/// <summary>
/// Abstraction over wall-clock time. Implementations in <c>CCE.Infrastructure</c> use
/// <see cref="DateTimeOffset.UtcNow"/>; tests supply a fake that advances time explicitly.
/// Every domain/application operation that needs 'now' takes <see cref="ISystemClock"/>.
/// Never call <c>DateTimeOffset.UtcNow</c> directly from domain or application layers.
/// </summary>
public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}
