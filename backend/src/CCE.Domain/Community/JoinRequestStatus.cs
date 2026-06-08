namespace CCE.Domain.Community;

/// <summary>Lifecycle of a request to join a private community.</summary>
public enum JoinRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
}
