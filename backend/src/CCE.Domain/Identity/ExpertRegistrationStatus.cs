namespace CCE.Domain.Identity;

/// <summary>
/// Lifecycle status of an <see cref="ExpertRegistrationRequest"/>.
/// </summary>
public enum ExpertRegistrationStatus
{
    /// <summary>Awaiting admin review. Initial state.</summary>
    Pending = 0,

    /// <summary>Admin approved; an <c>ExpertProfile</c> was created. Terminal.</summary>
    Approved = 1,

    /// <summary>Admin rejected; rejection reason recorded. Terminal.</summary>
    Rejected = 2,
}
