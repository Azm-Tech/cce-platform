using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.Domain.Country.Events;

namespace CCE.Domain.Country;

/// <summary>
/// State-rep submission asking the center to publish country-scoped content.
    /// Supports Resources, News articles, and Events via <see cref="ContentType"/>.
/// State machine: <c>Pending → Approved</c> or <c>Pending → Rejected</c> (terminal).
/// Approving raises <see cref="CountryContentRequestApprovedEvent"/> which a future
/// handler (Sprint-07 / US050) routes to create the actual content aggregate.
/// </summary>
[Audited]
public sealed class CountryContentRequest : AggregateRoot<System.Guid>
{
    private CountryContentRequest(
        System.Guid id,
        System.Guid countryId,
        System.Guid requestedById,
        ContentType type,
        string proposedTitleAr,
        string proposedTitleEn,
        string proposedDescriptionAr,
        string proposedDescriptionEn,
        ResourceType? proposedResourceType,
        System.Guid? proposedAssetFileId,
        System.Guid? proposedTopicId,
        System.DateTimeOffset? proposedStartsOn,
        System.DateTimeOffset? proposedEndsOn,
        string? proposedLocationAr,
        string? proposedLocationEn,
        string? proposedOnlineMeetingUrl,
        System.DateTimeOffset submittedOn) : base(id)
    {
        CountryId = countryId;
        RequestedById = requestedById;
        Type = type;
        ProposedTitleAr = proposedTitleAr;
        ProposedTitleEn = proposedTitleEn;
        ProposedDescriptionAr = proposedDescriptionAr;
        ProposedDescriptionEn = proposedDescriptionEn;
        ProposedResourceType = proposedResourceType;
        ProposedAssetFileId = proposedAssetFileId;
        ProposedTopicId = proposedTopicId;
        ProposedStartsOn = proposedStartsOn;
        ProposedEndsOn = proposedEndsOn;
        ProposedLocationAr = proposedLocationAr;
        ProposedLocationEn = proposedLocationEn;
        ProposedOnlineMeetingUrl = proposedOnlineMeetingUrl;
        SubmittedOn = submittedOn;
        Status = CountryContentRequestStatus.Pending;
    }

    public System.Guid CountryId { get; private set; }
    public System.Guid RequestedById { get; private set; }
    public ContentType Type { get; private set; }
    public CountryContentRequestStatus Status { get; private set; }
    public string ProposedTitleAr { get; private set; } = string.Empty;
    public string ProposedTitleEn { get; private set; } = string.Empty;
    public string ProposedDescriptionAr { get; private set; } = string.Empty;
    public string ProposedDescriptionEn { get; private set; } = string.Empty;

    // Resource-specific (null for News/Event)
    public ResourceType? ProposedResourceType { get; private set; }
    public System.Guid? ProposedAssetFileId { get; private set; }

    // News/Event-specific
    public System.Guid? ProposedTopicId { get; private set; }

    // Event-specific
    public System.DateTimeOffset? ProposedStartsOn { get; private set; }
    public System.DateTimeOffset? ProposedEndsOn { get; private set; }
    public string? ProposedLocationAr { get; private set; }
    public string? ProposedLocationEn { get; private set; }
    public string? ProposedOnlineMeetingUrl { get; private set; }

    public System.DateTimeOffset SubmittedOn { get; private set; }
    public string? AdminNotesAr { get; private set; }
    public string? AdminNotesEn { get; private set; }
    public System.Guid? ProcessedById { get; private set; }
    public System.DateTimeOffset? ProcessedOn { get; private set; }

    // ─── Factories ────────────────────────────────────────────────────────────

    public static CountryContentRequest SubmitResource(
        System.Guid countryId,
        System.Guid requestedById,
        string titleAr, string titleEn,
        string descriptionAr, string descriptionEn,
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
        return new CountryContentRequest(
            System.Guid.NewGuid(), countryId, requestedById,
            ContentType.Resource,
            titleAr, titleEn, descriptionAr, descriptionEn,
            resourceType, assetFileId,
            null, null, null, null, null, null,
            clock.UtcNow);
    }

    public static CountryContentRequest SubmitNews(
        System.Guid countryId,
        System.Guid requestedById,
        string titleAr, string titleEn,
        string contentAr, string contentEn,
        System.Guid topicId,
        System.Guid? featuredImageAssetId,
        ISystemClock clock)
    {
        if (countryId == System.Guid.Empty) throw new DomainException("CountryId is required.");
        if (requestedById == System.Guid.Empty) throw new DomainException("RequestedById is required.");
        if (string.IsNullOrWhiteSpace(titleAr)) throw new DomainException("TitleAr is required.");
        if (string.IsNullOrWhiteSpace(titleEn)) throw new DomainException("TitleEn is required.");
        if (string.IsNullOrWhiteSpace(contentAr)) throw new DomainException("ContentAr is required.");
        if (string.IsNullOrWhiteSpace(contentEn)) throw new DomainException("ContentEn is required.");
        if (topicId == System.Guid.Empty) throw new DomainException("TopicId is required.");
        return new CountryContentRequest(
            System.Guid.NewGuid(), countryId, requestedById,
            ContentType.News,
            titleAr, titleEn, contentAr, contentEn,
            null, featuredImageAssetId,
            topicId, null, null, null, null, null,
            clock.UtcNow);
    }

    public static CountryContentRequest SubmitEvent(
        System.Guid countryId,
        System.Guid requestedById,
        string titleAr, string titleEn,
        string descriptionAr, string descriptionEn,
        System.Guid topicId,
        System.DateTimeOffset startsOn,
        System.DateTimeOffset endsOn,
        string? locationAr,
        string? locationEn,
        string? onlineMeetingUrl,
        ISystemClock clock)
    {
        if (countryId == System.Guid.Empty) throw new DomainException("CountryId is required.");
        if (requestedById == System.Guid.Empty) throw new DomainException("RequestedById is required.");
        if (string.IsNullOrWhiteSpace(titleAr)) throw new DomainException("TitleAr is required.");
        if (string.IsNullOrWhiteSpace(titleEn)) throw new DomainException("TitleEn is required.");
        if (string.IsNullOrWhiteSpace(descriptionAr)) throw new DomainException("DescriptionAr is required.");
        if (string.IsNullOrWhiteSpace(descriptionEn)) throw new DomainException("DescriptionEn is required.");
        if (topicId == System.Guid.Empty) throw new DomainException("TopicId is required.");
        if (startsOn >= endsOn) throw new DomainException("StartsOn must be before EndsOn.");
        return new CountryContentRequest(
            System.Guid.NewGuid(), countryId, requestedById,
            ContentType.Event,
            titleAr, titleEn, descriptionAr, descriptionEn,
            null, null,
            topicId, startsOn, endsOn, locationAr, locationEn, onlineMeetingUrl,
            clock.UtcNow);
    }

    // ─── State transitions ─────────────────────────────────────────────────────

    public void Approve(System.Guid approvedById, string? notesAr, string? notesEn, ISystemClock clock)
    {
        if (Status != CountryContentRequestStatus.Pending)
            throw new DomainException($"Cannot approve a {Status} request — only Pending allowed.");
        if (approvedById == System.Guid.Empty) throw new DomainException("ApprovedById is required.");
        var now = clock.UtcNow;
        Status = CountryContentRequestStatus.Approved;
        ProcessedById = approvedById;
        ProcessedOn = now;
        AdminNotesAr = notesAr;
        AdminNotesEn = notesEn;
        RaiseDomainEvent(new CountryContentRequestApprovedEvent(
            Id, CountryId, RequestedById, Type, approvedById, now));
    }

    public void Reject(System.Guid rejectedById, string notesAr, string notesEn, ISystemClock clock)
    {
        if (Status != CountryContentRequestStatus.Pending)
            throw new DomainException($"Cannot reject a {Status} request — only Pending allowed.");
        if (rejectedById == System.Guid.Empty) throw new DomainException("RejectedById is required.");
        if (string.IsNullOrWhiteSpace(notesAr)) throw new DomainException("Arabic admin notes are required to reject.");
        if (string.IsNullOrWhiteSpace(notesEn)) throw new DomainException("English admin notes are required to reject.");
        var now = clock.UtcNow;
        Status = CountryContentRequestStatus.Rejected;
        ProcessedById = rejectedById;
        ProcessedOn = now;
        AdminNotesAr = notesAr;
        AdminNotesEn = notesEn;
        RaiseDomainEvent(new CountryContentRequestRejectedEvent(
            Id, CountryId, RequestedById, Type, rejectedById, notesAr, notesEn, now));
    }
}
