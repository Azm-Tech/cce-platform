namespace CCE.Domain.Community;

/// <summary>
/// A user's vote intent on a post or reply. The integer value doubles as the stored
/// vote weight: <c>Up = +1</c>, <c>Down = -1</c>, <c>None = 0</c> (retract an existing vote).
/// </summary>
public enum VoteDirection
{
    Down = -1,
    None = 0,
    Up = 1,
}
