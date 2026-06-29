using CCE.Domain.Common;

namespace CCE.Domain.Community;

/// <summary>
/// Immutable audit-log row written once per moderation decision (automated or human).
/// Never updated after insert — append a new row for each re-moderation or human override.
/// </summary>
public sealed class ModerationRecord : Entity<System.Guid>
{
    private ModerationRecord(
        System.Guid id,
        ModerationContentType contentType,
        System.Guid contentId,
        ModerationStatus status,
        string phase,
        string? provider,
        float? score,
        string? category,
        string? reason,
        System.Guid? reviewedById) : base(id)
    {
        ContentType  = contentType;
        ContentId    = contentId;
        Status       = status;
        Phase        = phase;
        Provider     = provider;
        Score        = score;
        Category     = category;
        Reason       = reason;
        ReviewedById = reviewedById;
        CreatedOn    = System.DateTimeOffset.UtcNow;
    }

    public ModerationContentType ContentType  { get; private set; }
    public System.Guid           ContentId    { get; private set; }
    public ModerationStatus      Status       { get; private set; }

    /// <summary>"rule" | "ai" | "human"</summary>
    public string  Phase        { get; private set; }
    public string? Provider     { get; private set; }
    public float?  Score        { get; private set; }
    public string? Category     { get; private set; }
    public string? Reason       { get; private set; }

    /// <summary>Null for automated (rule/ai) decisions.</summary>
    public System.Guid?         ReviewedById { get; private set; }
    public System.DateTimeOffset CreatedOn   { get; private set; }

    public static ModerationRecord CreateAutomated(
        ModerationContentType contentType,
        System.Guid contentId,
        ModerationStatus status,
        string phase,
        string? provider,
        float? score,
        string? category,
        string? reason)
        => new(System.Guid.NewGuid(), contentType, contentId, status, phase, provider, score, category, reason, null);

    public static ModerationRecord CreateHuman(
        ModerationContentType contentType,
        System.Guid contentId,
        ModerationStatus status,
        string? reason,
        System.Guid reviewedById)
        => new(System.Guid.NewGuid(), contentType, contentId, status, "human", null, null, null, reason, reviewedById);
}
