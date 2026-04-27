using CCE.Domain.Common;

namespace CCE.Domain.Country;

/// <summary>
/// Admin-managed bilingual profile content for a <see cref="Country"/>. 1:1 — enforced by
/// unique index on <see cref="CountryId"/> in Phase 08. <see cref="RowVersion"/> for
/// optimistic concurrency on edit.
/// </summary>
[Audited]
public sealed class CountryProfile : Entity<System.Guid>
{
    private CountryProfile(
        System.Guid id,
        System.Guid countryId,
        string descriptionAr,
        string descriptionEn,
        string keyInitiativesAr,
        string keyInitiativesEn,
        string? contactInfoAr,
        string? contactInfoEn,
        System.Guid lastUpdatedById,
        System.DateTimeOffset lastUpdatedOn) : base(id)
    {
        CountryId = countryId;
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        KeyInitiativesAr = keyInitiativesAr;
        KeyInitiativesEn = keyInitiativesEn;
        ContactInfoAr = contactInfoAr;
        ContactInfoEn = contactInfoEn;
        LastUpdatedById = lastUpdatedById;
        LastUpdatedOn = lastUpdatedOn;
    }

    public System.Guid CountryId { get; private set; }
    public string DescriptionAr { get; private set; }
    public string DescriptionEn { get; private set; }
    public string KeyInitiativesAr { get; private set; }
    public string KeyInitiativesEn { get; private set; }
    public string? ContactInfoAr { get; private set; }
    public string? ContactInfoEn { get; private set; }
    public System.Guid LastUpdatedById { get; private set; }
    public System.DateTimeOffset LastUpdatedOn { get; private set; }
    public byte[] RowVersion { get; private set; } = System.Array.Empty<byte>();

    public static CountryProfile Create(
        System.Guid countryId,
        string descriptionAr,
        string descriptionEn,
        string keyInitiativesAr,
        string keyInitiativesEn,
        string? contactInfoAr,
        string? contactInfoEn,
        System.Guid createdById,
        ISystemClock clock)
    {
        if (countryId == System.Guid.Empty) throw new DomainException("CountryId is required.");
        if (string.IsNullOrWhiteSpace(descriptionAr)) throw new DomainException("DescriptionAr is required.");
        if (string.IsNullOrWhiteSpace(descriptionEn)) throw new DomainException("DescriptionEn is required.");
        if (string.IsNullOrWhiteSpace(keyInitiativesAr)) throw new DomainException("KeyInitiativesAr is required.");
        if (string.IsNullOrWhiteSpace(keyInitiativesEn)) throw new DomainException("KeyInitiativesEn is required.");
        if (createdById == System.Guid.Empty) throw new DomainException("CreatedById is required.");
        return new CountryProfile(
            id: System.Guid.NewGuid(),
            countryId: countryId,
            descriptionAr: descriptionAr,
            descriptionEn: descriptionEn,
            keyInitiativesAr: keyInitiativesAr,
            keyInitiativesEn: keyInitiativesEn,
            contactInfoAr: contactInfoAr,
            contactInfoEn: contactInfoEn,
            lastUpdatedById: createdById,
            lastUpdatedOn: clock.UtcNow);
    }

    public void Update(
        string descriptionAr,
        string descriptionEn,
        string keyInitiativesAr,
        string keyInitiativesEn,
        string? contactInfoAr,
        string? contactInfoEn,
        System.Guid updatedById,
        ISystemClock clock)
    {
        if (string.IsNullOrWhiteSpace(descriptionAr)) throw new DomainException("DescriptionAr is required.");
        if (string.IsNullOrWhiteSpace(descriptionEn)) throw new DomainException("DescriptionEn is required.");
        if (string.IsNullOrWhiteSpace(keyInitiativesAr)) throw new DomainException("KeyInitiativesAr is required.");
        if (string.IsNullOrWhiteSpace(keyInitiativesEn)) throw new DomainException("KeyInitiativesEn is required.");
        if (updatedById == System.Guid.Empty) throw new DomainException("UpdatedById is required.");
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        KeyInitiativesAr = keyInitiativesAr;
        KeyInitiativesEn = keyInitiativesEn;
        ContactInfoAr = contactInfoAr;
        ContactInfoEn = contactInfoEn;
        LastUpdatedById = updatedById;
        LastUpdatedOn = clock.UtcNow;
    }
}
