using CCE.Domain.Common;

namespace CCE.TestInfrastructure.Time;

/// <summary>
/// Deterministic <see cref="ISystemClock"/> fake for unit tests.
/// Construct with an explicit <see cref="DateTimeOffset"/> (or default to a fixed reference moment),
/// then advance with <see cref="Advance"/> as the test demands.
/// Thread-safe for the simple "set once, advance under lock, read" pattern most tests use.
/// </summary>
public sealed class FakeSystemClock : ISystemClock
{
    /// <summary>
    /// Default reference moment: 2026-01-01T00:00:00Z. Picked deliberately as a non-DST,
    /// non-leap-second, non-edge timestamp. Tests that don't care about absolute time start here.
    /// </summary>
    public static DateTimeOffset DefaultStart { get; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly object _gate = new();
    private DateTimeOffset _now;

    public FakeSystemClock() : this(DefaultStart) { }

    public FakeSystemClock(DateTimeOffset start) => _now = start;

    /// <inheritdoc />
    public DateTimeOffset UtcNow
    {
        get
        {
            lock (_gate)
            {
                return _now;
            }
        }
    }

    /// <summary>
    /// Advance the clock by the given duration. Negative durations are rejected — going
    /// backwards in tests usually masks a real bug; if you need to assert "at time T-1s, X
    /// hadn't yet happened," construct two clocks instead.
    /// </summary>
    public void Advance(TimeSpan delta)
    {
        if (delta < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(delta), delta, "Cannot rewind the clock.");
        }
        lock (_gate)
        {
            _now = _now.Add(delta);
        }
    }

    /// <summary>
    /// Set the clock to an absolute moment. Useful when a test starts mid-scenario.
    /// </summary>
    public void SetTo(DateTimeOffset moment)
    {
        lock (_gate)
        {
            _now = moment;
        }
    }
}
