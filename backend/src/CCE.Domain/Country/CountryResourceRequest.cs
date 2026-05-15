using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.Domain.Country.Events;

namespace CCE.Domain.Country;

/// <summary>
/// State-rep submission asking the center to publish a country-scoped resource. State machine:
/// <c>Pending → Approved</c> or <c>Pending → Rejected</c> (terminal). Approving raises
/// <see cref="CountryResourceRequestApprovedEvent"/> which Phase 07 routes to a handler that
/// creates the actual <c>Resource</c>.
/// </summary>
[Audited]
public sealed class CountryResourceRequest : SoftDeletableAggregateRoot<System.Guid>
{
    private CountryResourceRequest(
        System.Guid id,
        System.Guid countryId,
        System.Guid requestedById,
        string proposedTitleAr,
        string proposedTitleEn,
        string proposedDescriptionAr,
        string proposedDescriptionEn,
        ResourceType proposedResourceType,
        System.Guid proposedAssetFileId,
        System.DateTimeOffset submittedOn) : base(id)
    {
        CountryId = countryId;
        RequestedById = requestedById;
        ProposedTitleAr = proposedTitleAr;
        ProposedTitleEn = proposedTitleEn;
        ProposedDescriptionAr = proposedDescriptionAr;
        ProposedDescriptionEn = proposedDescriptionEn;
        ProposedResourceType = proposedResourceType;
        ProposedAssetFileId = proposedAssetFileId;
        SubmittedOn = submittedOn;
        Status = CountryResourceRequestStatus.Pending;
    }

    public System.Guid CountryId { get; private set; }
    public System.Guid RequestedById { get; private set; }
    public CountryResourceRequestStatus Status { get; private set; }
    public string ProposedTitleAr { get; private set; }
    public string ProposedTitleEn { get; private set; }
    public string ProposedDescriptionAr { get; private set; }
    public string ProposedDescriptionEn { get; private set; }
    public ResourceType ProposedResourceType { get; private set; }
    public System.Guid ProposedAssetFileId { get; private set; }
    public System.DateTimeOffset SubmittedOn { get; private set; }
    public string? AdminNotesAr { get; private set; }
    public string? AdminNotesEn { get; private set; }
    public System.Guid? ProcessedById { get; private set; }
    public System.DateTimeOffset? ProcessedOn { get; private set; }
    public static CountryResourceRequest Submit(
        System.Guid countryId,
        System.Guid requestedById,
        string titleAr,
        string titleEn,
        string descriptionAr,
        string descriptionEn,
        ResourceType resourceType,
        System.Guid assetFileId,
        ISystemClock clock)
    {
        if (countryId == System.Guid.Empty) throw new DomainException("CountryId is required.");
        if (requestedById == System.Guid.Empty) throw new DomainException("RequestedById is required.");
        if (string.IsNullOrWhiteSpace(titleAr)) throw new DomainException("TitleAr is required.");
        if (string.IsNullOrWhiteSpace(titleEn)) throw new DomainException("TitleEn is required.");
        if (string.IsNullOrWhiteSpace(descriptionAr)) throw new DomainException("DescriptionAr is required.");
        if (string.IsNullOrWhiteSpace(descriptionEn)) throw new DomainException("DescriptionEn is required.");
        if (assetFileId == System.Guid.Empty) throw new DomainException("AssetFileId is required.");
        return new CountryResourceRequest(
            System.Guid.NewGuid(), countryId, requestedById,
            titleAr, titleEn, descriptionAr, descriptionEn,
            resourceType, assetFileId, clock.UtcNow);
    }

    public void Approve(System.Guid approvedById, string? notesAr, string? notesEn, ISystemClock clock)
    {
        if (Status != CountryResourceRequestStatus.Pending)
        {
            throw new DomainException($"Cannot approve a {Status} request — only Pending allowed.");
        }
        if (approvedById == System.Guid.Empty) throw new DomainException("ApprovedById is required.");
        var now = clock.UtcNow;
        Status = CountryResourceRequestStatus.Approved;
        ProcessedById = approvedById;
        ProcessedOn = now;
        AdminNotesAr = notesAr;
        AdminNotesEn = notesEn;
        RaiseDomainEvent(new CountryResourceRequestApprovedEvent(
            Id, CountryId, RequestedById, approvedById, now));
    }

    public void Reject(System.Guid rejectedById, string notesAr, string notesEn, ISystemClock clock)
    {
        if (Status != CountryResourceRequestStatus.Pending)
        {
            throw new DomainException($"Cannot reject a {Status} request — only Pending allowed.");
        }
        if (rejectedById == System.Guid.Empty) throw new DomainException("RejectedById is required.");
        if (string.IsNullOrWhiteSpace(notesAr)) throw new DomainException("Arabic admin notes are required to reject.");
        if (string.IsNullOrWhiteSpace(notesEn)) throw new DomainException("English admin notes are required to reject.");
        var now = clock.UtcNow;
        Status = CountryResourceRequestStatus.Rejected;
        ProcessedById = rejectedById;
        ProcessedOn = now;
        AdminNotesAr = notesAr;
        AdminNotesEn = notesEn;
        RaiseDomainEvent(new CountryResourceRequestRejectedEvent(
            Id, CountryId, RequestedById, rejectedById, notesAr, notesEn, now));
    }
}
