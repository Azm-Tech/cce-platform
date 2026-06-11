namespace CCE.Application.Community.Commands;

/// <summary>Desired follow-relationship state carried by a follow upsert (PUT).</summary>
public enum FollowStatus
{
    Followed = 0,
    Unfollowed = 1,
}
