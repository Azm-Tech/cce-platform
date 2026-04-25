using CCE.Domain.Common;

namespace CCE.Domain.Audit;

/// <summary>
/// Single immutable record of a security-relevant action. Append-only — never updated, never deleted.
/// Persistence enforces append-only via SQL trigger in <c>CCE.Infrastructure</c> (Phase 06).
/// </summary>
public sealed class AuditEvent : Entity<Guid>
{
    /// <summary>EF Core constructor — bypasses validation. Application code must use the public constructor.</summary>
#pragma warning disable CS8618 // Non-nullable members initialized by EF Core during materialization.
    private AuditEvent(Guid id) : base(id) { }
#pragma warning restore CS8618

    public AuditEvent(
        Guid id,
        DateTimeOffset occurredOn,
        string actor,
        string action,
        string resource,
        Guid correlationId,
        string? diff)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(actor))
        {
            throw new ArgumentException("Actor cannot be null or whitespace.", nameof(actor));
        }
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Action cannot be null or whitespace.", nameof(action));
        }
        if (string.IsNullOrWhiteSpace(resource))
        {
            throw new ArgumentException("Resource cannot be null or whitespace.", nameof(resource));
        }

        OccurredOn = occurredOn;
        Actor = actor;
        Action = action;
        Resource = resource;
        CorrelationId = correlationId;
        Diff = diff;
    }

    /// <summary>UTC moment the audited action occurred.</summary>
    public DateTimeOffset OccurredOn { get; private set; }

    /// <summary>Identity of the principal that performed the action (typically <c>upn</c> claim).</summary>
    public string Actor { get; private set; } = null!;

    /// <summary>Verb describing the action — convention <c>Resource.Verb</c> (e.g., <c>User.Create</c>).</summary>
    public string Action { get; private set; } = null!;

    /// <summary>Stable resource identifier (e.g., <c>User/abc-123</c>).</summary>
    public string Resource { get; private set; } = null!;

    /// <summary>Cross-system correlation identifier connecting this event to logs, traces, and other events.</summary>
    public Guid CorrelationId { get; private set; }

    /// <summary>Optional JSON describing the state change. Null for actions without a payload (e.g., logins).</summary>
    public string? Diff { get; private set; }
}
