namespace CCE.Application.Community.Commands;

/// <summary>Desired follow-relationship state carried by a follow upsert (PUT).</summary>
public enum FollowStatus
{
    Unfollowed = 0,
    Followed = 1,
}
