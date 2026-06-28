namespace CCE.Domain.Identity;

public sealed class PermissionAuditLog
{
    public long Id { get; private set; }
    public DateTimeOffset ChangedAtUtc { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public string ChangedByEmail { get; private set; }
    public string RoleName { get; private set; }
    public string PermissionName { get; private set; }
    public PermissionAuditAction Action { get; private set; }

    private PermissionAuditLog() { ChangedByEmail = ""; RoleName = ""; PermissionName = ""; }

    public static PermissionAuditLog Record(
        DateTimeOffset now,
        Guid actorId,
        string actorEmail,
        string role,
        string permission,
        PermissionAuditAction action) => new()
    {
        ChangedAtUtc    = now,
        ChangedByUserId = actorId,
        ChangedByEmail  = actorEmail,
        RoleName        = role,
        PermissionName  = permission,
        Action          = action,
    };
}

public enum PermissionAuditAction
{
    None    = 0,
    Granted = 1,
    Revoked = 2,
}
