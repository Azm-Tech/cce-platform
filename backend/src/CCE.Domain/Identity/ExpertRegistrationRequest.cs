using CCE.Domain.Common;
using CCE.Domain.Identity.Events;

namespace CCE.Domain.Identity;

/// <summary>
/// Workflow record for a registered user requesting expert status. Aggregate root —
/// the <see cref="Approve"/> transition raises an <see cref="ExpertRegistrationApprovedEvent"/>
/// that Phase 07's domain-event dispatcher routes to an in-process handler which creates
/// the corresponding <c>ExpertProfile</c>. Soft-deletable for admin recovery flows.
/// </summary>
[Audited]
public sealed class ExpertRegistrationRequest : SoftDeletableAggregateRoot<System.Guid>
{
    private ExpertRegistrationRequest(
        System.Guid id,
        System.Guid requestedById,
        string requestedBioAr,
        string requestedBioEn,
        IList<string> requestedTags,
        System.DateTimeOffset submittedOn) : base(id)
    {
        RequestedById = requestedById;
        RequestedBioAr = requestedBioAr;
        RequestedBioEn = requestedBioEn;
        RequestedTags = requestedTags;
        SubmittedOn = submittedOn;
        Status = ExpertRegistrationStatus.Pending;
    }

    public System.Guid RequestedById { get; private set; }

    public string RequestedBioAr { get; private set; } = string.Empty;

    public string RequestedBioEn { get; private set; } = string.Empty;

    public IList<string> RequestedTags { get; private set; } = new List<string>();

    public System.DateTimeOffset SubmittedOn { get; private set; }

    public ExpertRegistrationStatus Status { get; private set; }

    public System.Guid? ProcessedById { get; private set; }

    public System.DateTimeOffset? ProcessedOn { get; private set; }

    public string? RejectionReasonAr { get; private set; }

    public string? RejectionReasonEn { get; private set; }

    /// <summary>
    /// Submit a new pending registration request. Validates inputs and records the submission moment.
    /// </summary>
    public static ExpertRegistrationRequest Submit(
        System.Guid requesterId,
        string bioAr,
        string bioEn,
        IEnumerable<string> tags,
        ISystemClock clock)
    {
        if (requesterId == System.Guid.Empty)
        {
            throw new DomainException("RequesterId is required.");
        }
        if (string.IsNullOrWhiteSpace(bioAr))
        {
            throw new DomainException("Arabic bio is required.");
        }
        if (string.IsNullOrWhiteSpace(bioEn))
        {
            throw new DomainException("English bio is required.");
        }
        var tagList = (tags ?? throw new DomainException("Tags collection is required."))
            .Select(static s => s?.Trim() ?? string.Empty)
            .Where(static s => s.Length > 0)
            .Distinct()
            .ToList();
        return new ExpertRegistrationRequest(
            id: System.Guid.NewGuid(),
            requestedById: requesterId,
            requestedBioAr: bioAr,
            requestedBioEn: bioEn,
            requestedTags: tagList,
            submittedOn: clock.UtcNow);
    }

    /// <summary>
    /// Admin approval transition. Allowed only from <see cref="ExpertRegistrationStatus.Pending"/>.
    /// Raises an <see cref="ExpertRegistrationApprovedEvent"/>.
    /// </summary>
    public void Approve(System.Guid approvedById, ISystemClock clock)
    {
        if (Status != ExpertRegistrationStatus.Pending)
        {
            throw new DomainException($"Cannot approve a {Status} request — only Pending allowed.");
        }
        if (approvedById == System.Guid.Empty)
        {
            throw new DomainException("ApprovedById is required.");
        }
        var now = clock.UtcNow;
        Status = ExpertRegistrationStatus.Approved;
        ProcessedById = approvedById;
        ProcessedOn = now;
        RaiseDomainEvent(new ExpertRegistrationApprovedEvent(
            RequestId: Id,
            RequestedById: RequestedById,
            ApprovedById: approvedById,
            RequestedBioAr: RequestedBioAr,
            RequestedBioEn: RequestedBioEn,
            RequestedTags: RequestedTags,
            OccurredOn: now));
    }

    /// <summary>
    /// Admin rejection transition with bilingual reason. Allowed only from
    /// <see cref="ExpertRegistrationStatus.Pending"/>. Raises an
    /// <see cref="ExpertRegistrationRejectedEvent"/>.
    /// </summary>
    public void Reject(System.Guid rejectedById, string reasonAr, string reasonEn, ISystemClock clock)
    {
        if (Status != ExpertRegistrationStatus.Pending)
        {
            throw new DomainException($"Cannot reject a {Status} request — only Pending allowed.");
        }
        if (rejectedById == System.Guid.Empty)
        {
            throw new DomainException("RejectedById is required.");
        }
        if (string.IsNullOrWhiteSpace(reasonAr))
        {
            throw new DomainException("Arabic rejection reason is required.");
        }
        if (string.IsNullOrWhiteSpace(reasonEn))
        {
            throw new DomainException("English rejection reason is required.");
        }
        var now = clock.UtcNow;
        Status = ExpertRegistrationStatus.Rejected;
        ProcessedById = rejectedById;
        ProcessedOn = now;
        RejectionReasonAr = reasonAr;
        RejectionReasonEn = reasonEn;
        RaiseDomainEvent(new ExpertRegistrationRejectedEvent(
            RequestId: Id,
            RequestedById: RequestedById,
            RejectedById: rejectedById,
            RejectionReasonAr: reasonAr,
            RejectionReasonEn: reasonEn,
            OccurredOn: now));
    }
}
