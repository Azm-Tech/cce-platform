using CCE.Domain.Common;
using CCE.Domain.Content.Events;

namespace CCE.Domain.Content;

/// <summary>
/// Knowledge-center resource (PDF, video, image, link, document). Aggregate root.
/// <see cref="CountryId"/> discriminates: <c>null</c> means center-managed (uploaded by
/// admin/content-manager), non-null means country-uploaded (by state rep, scoped to that
/// country). Soft-deletable. <see cref="RowVersion"/> is set by EF on update via
/// <c>[Timestamp]</c> mapping in Phase 07.
/// </summary>
[Audited]
public sealed class Resource : SoftDeletableAggregateRoot<System.Guid>
{
    private Resource(
        System.Guid id,
        string titleAr,
        string titleEn,
        string descriptionAr,
        string descriptionEn,
        ResourceType resourceType,
        System.Guid categoryId,
        System.Guid? countryId,
        System.Guid uploadedById,
        System.Guid assetFileId) : base(id)
    {
        TitleAr = titleAr;
        TitleEn = titleEn;
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        ResourceType = resourceType;
        CategoryId = categoryId;
        CountryId = countryId;
        UploadedById = uploadedById;
        AssetFileId = assetFileId;
    }

    public string TitleAr { get; private set; }
    public string TitleEn { get; private set; }
    public string DescriptionAr { get; private set; }
    public string DescriptionEn { get; private set; }
    public ResourceType ResourceType { get; private set; }
    public System.Guid CategoryId { get; private set; }
    public System.Guid? CountryId { get; private set; }
    public System.Guid UploadedById { get; private set; }
    public System.Guid AssetFileId { get; private set; }
    public System.DateTimeOffset? PublishedOn { get; private set; }
    public long ViewCount { get; private set; }

    /// <summary>EF-managed concurrency token (rowversion).</summary>
    public byte[] RowVersion { get; private set; } = System.Array.Empty<byte>();

    /// <summary>True when no country owns this resource (center-managed).</summary>
    public bool IsCenterManaged => CountryId is null;

    /// <summary>True when the resource has been published at least once.</summary>
    public bool IsPublished => PublishedOn is not null;

    public static Resource Draft(
        string titleAr,
        string titleEn,
        string descriptionAr,
        string descriptionEn,
        ResourceType resourceType,
        System.Guid categoryId,
        System.Guid? countryId,
        System.Guid uploadedById,
        System.Guid assetFileId,
        ISystemClock clock)
    {
        _ = clock;
        if (string.IsNullOrWhiteSpace(titleAr)) throw new DomainException("TitleAr is required.");
        if (string.IsNullOrWhiteSpace(titleEn)) throw new DomainException("TitleEn is required.");
        if (string.IsNullOrWhiteSpace(descriptionAr)) throw new DomainException("DescriptionAr is required.");
        if (string.IsNullOrWhiteSpace(descriptionEn)) throw new DomainException("DescriptionEn is required.");
        if (categoryId == System.Guid.Empty) throw new DomainException("CategoryId is required.");
        if (uploadedById == System.Guid.Empty) throw new DomainException("UploadedById is required.");
        if (assetFileId == System.Guid.Empty) throw new DomainException("AssetFileId is required.");
        return new Resource(
            id: System.Guid.NewGuid(),
            titleAr: titleAr,
            titleEn: titleEn,
            descriptionAr: descriptionAr,
            descriptionEn: descriptionEn,
            resourceType: resourceType,
            categoryId: categoryId,
            countryId: countryId,
            uploadedById: uploadedById,
            assetFileId: assetFileId);
    }

    public void Publish(ISystemClock clock)
    {
        if (IsPublished)
        {
            return;
        }
        PublishedOn = clock.UtcNow;
        RaiseDomainEvent(new ResourcePublishedEvent(
            ResourceId: Id,
            CountryId: CountryId,
            CategoryId: CategoryId,
            OccurredOn: PublishedOn.Value));
    }

    /// <summary>
    /// Mutates the editable content fields. Audited via the existing AuditingInterceptor.
    /// </summary>
    public void UpdateContent(
        string titleAr,
        string titleEn,
        string descriptionAr,
        string descriptionEn,
        ResourceType resourceType,
        System.Guid categoryId)
    {
        if (string.IsNullOrWhiteSpace(titleAr)) throw new DomainException("TitleAr is required.");
        if (string.IsNullOrWhiteSpace(titleEn)) throw new DomainException("TitleEn is required.");
        if (string.IsNullOrWhiteSpace(descriptionAr)) throw new DomainException("DescriptionAr is required.");
        if (string.IsNullOrWhiteSpace(descriptionEn)) throw new DomainException("DescriptionEn is required.");
        if (categoryId == System.Guid.Empty) throw new DomainException("CategoryId is required.");
        TitleAr = titleAr;
        TitleEn = titleEn;
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        ResourceType = resourceType;
        CategoryId = categoryId;
    }

    public void IncrementViewCount() => ViewCount++;
}
