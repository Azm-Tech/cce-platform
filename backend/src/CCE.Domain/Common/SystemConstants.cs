namespace CCE.Domain.Common;

/// <summary>
/// Well-known sentinel values used across the domain.
/// </summary>
public static class SystemConstants
{
    /// <summary>
    /// Represents an anonymous or system actor when no real user is available.
    /// Used for audit fields (CreatedById, LastModifiedById) on entities
    /// created by unauthenticated users.
    /// </summary>
    public static readonly Guid AnonymousUserId = new("00000000-0000-0000-0000-000000000001");
}
