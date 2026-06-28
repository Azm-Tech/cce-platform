using CCE.Domain.Common;

namespace CCE.Domain.Country;

/// <summary>
/// Admin/state-rep managed profile for a <see cref="Country"/>. 1:1 — enforced by
/// unique index on <see cref="CountryId"/>. <see cref="RowVersion"/> for optimistic
/// concurrency on edit.
/// Demographic fields (Population, AreaSqKm, GdpPerCapita, NdcAssetId) are nullable
/// at the DB level so legacy rows without data remain valid; the domain enforces >0
/// on write when a value is supplied.
/// CCE Classification/Performance/TotalIndex are read-only — retrieved from
/// <see cref="CountryKapsarcSnapshot"/> and never stored here.
/// </summary>
[Audited]
public sealed class CountryProfile : AuditableEntity<System.Guid>
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
        int? population,
        decimal? areaSqKm,
        decimal? gdpPerCapita,
        System.Guid? nationallyDeterminedContributionAssetId) : base(id)
    {
        CountryId = countryId;
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        KeyInitiativesAr = keyInitiativesAr;
        KeyInitiativesEn = keyInitiativesEn;
        ContactInfoAr = contactInfoAr;
        ContactInfoEn = contactInfoEn;
        Population = population;
        AreaSqKm = areaSqKm;
        GdpPerCapita = gdpPerCapita;
        NationallyDeterminedContributionAssetId = nationallyDeterminedContributionAssetId;
    }

    public System.Guid CountryId { get; private set; }
    public string DescriptionAr { get; private set; }
    public string DescriptionEn { get; private set; }
    public string KeyInitiativesAr { get; private set; }
    public string KeyInitiativesEn { get; private set; }
    public string? ContactInfoAr { get; private set; }
    public string? ContactInfoEn { get; private set; }

    // ─── Demographic / economic fields (US061) ────────────────────────────────
    public int? Population { get; private set; }
    public decimal? AreaSqKm { get; private set; }
    public decimal? GdpPerCapita { get; private set; }
    public System.Guid? NationallyDeterminedContributionAssetId { get; private set; }

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
        ISystemClock clock,
        int? population = null,
        decimal? areaSqKm = null,
        decimal? gdpPerCapita = null,
        System.Guid? nationallyDeterminedContributionAssetId = null)
    {
        if (countryId == System.Guid.Empty) throw new DomainException("CountryId is required.");
        if (string.IsNullOrWhiteSpace(descriptionAr)) throw new DomainException("DescriptionAr is required.");
        if (string.IsNullOrWhiteSpace(descriptionEn)) throw new DomainException("DescriptionEn is required.");
        if (string.IsNullOrWhiteSpace(keyInitiativesAr)) throw new DomainException("KeyInitiativesAr is required.");
        if (string.IsNullOrWhiteSpace(keyInitiativesEn)) throw new DomainException("KeyInitiativesEn is required.");
        if (createdById == System.Guid.Empty) throw new DomainException("CreatedById is required.");
        if (population is not null && population <= 0) throw new DomainException("Population must be greater than 0.");
        if (areaSqKm is not null && areaSqKm <= 0) throw new DomainException("AreaSqKm must be greater than 0.");
        if (gdpPerCapita is not null && gdpPerCapita <= 0) throw new DomainException("GdpPerCapita must be greater than 0.");
        var p = new CountryProfile(
            id: System.Guid.NewGuid(),
            countryId: countryId,
            descriptionAr: descriptionAr,
            descriptionEn: descriptionEn,
            keyInitiativesAr: keyInitiativesAr,
            keyInitiativesEn: keyInitiativesEn,
            contactInfoAr: contactInfoAr,
            contactInfoEn: contactInfoEn,
            population: population,
            areaSqKm: areaSqKm,
            gdpPerCapita: gdpPerCapita,
            nationallyDeterminedContributionAssetId: nationallyDeterminedContributionAssetId);
        p.MarkAsCreated(createdById, clock);
        p.MarkAsModified(createdById, clock);
        return p;
    }

    /// <summary>
    /// Creates an empty profile shell for a country (used when a State Representative is
    /// assigned so a record exists to edit). Editorial content is left blank and filled
    /// later via <see cref="Update"/>, which enforces the required-field rules (US061).
    /// </summary>
    public static CountryProfile CreateDraft(
        System.Guid countryId,
        System.Guid createdById,
        ISystemClock clock)
    {
        if (countryId == System.Guid.Empty) throw new DomainException("CountryId is required.");
        if (createdById == System.Guid.Empty) throw new DomainException("CreatedById is required.");
        var p = new CountryProfile(
            id: System.Guid.NewGuid(),
            countryId: countryId,
            descriptionAr: string.Empty,
            descriptionEn: string.Empty,
            keyInitiativesAr: string.Empty,
            keyInitiativesEn: string.Empty,
            contactInfoAr: null,
            contactInfoEn: null,
            population: null,
            areaSqKm: null,
            gdpPerCapita: null,
            nationallyDeterminedContributionAssetId: null);
        p.MarkAsCreated(createdById, clock);
        p.MarkAsModified(createdById, clock);
        return p;
    }

    public void Update(
        string descriptionAr,
        string descriptionEn,
        string keyInitiativesAr,
        string keyInitiativesEn,
        string? contactInfoAr,
        string? contactInfoEn,
        System.Guid updatedById,
        ISystemClock clock,
        int? population = null,
        decimal? areaSqKm = null,
        decimal? gdpPerCapita = null,
        System.Guid? nationallyDeterminedContributionAssetId = null)
    {
        if (string.IsNullOrWhiteSpace(descriptionAr)) throw new DomainException("DescriptionAr is required.");
        if (string.IsNullOrWhiteSpace(descriptionEn)) throw new DomainException("DescriptionEn is required.");
        if (string.IsNullOrWhiteSpace(keyInitiativesAr)) throw new DomainException("KeyInitiativesAr is required.");
        if (string.IsNullOrWhiteSpace(keyInitiativesEn)) throw new DomainException("KeyInitiativesEn is required.");
        if (updatedById == System.Guid.Empty) throw new DomainException("UpdatedById is required.");
        if (population is not null && population <= 0) throw new DomainException("Population must be greater than 0.");
        if (areaSqKm is not null && areaSqKm <= 0) throw new DomainException("AreaSqKm must be greater than 0.");
        if (gdpPerCapita is not null && gdpPerCapita <= 0) throw new DomainException("GdpPerCapita must be greater than 0.");
        DescriptionAr = descriptionAr;
        DescriptionEn = descriptionEn;
        KeyInitiativesAr = keyInitiativesAr;
        KeyInitiativesEn = keyInitiativesEn;
        ContactInfoAr = contactInfoAr;
        ContactInfoEn = contactInfoEn;
        Population = population;
        AreaSqKm = areaSqKm;
        GdpPerCapita = gdpPerCapita;
        NationallyDeterminedContributionAssetId = nationallyDeterminedContributionAssetId;
        MarkAsModified(updatedById, clock);
    }
}
