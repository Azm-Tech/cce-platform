using System.Text.RegularExpressions;
using CCE.Domain.Common;

namespace CCE.Domain.Country;

/// <summary>
/// Country reference entity — primary identifier is ISO 3166-1 alpha-3 (e.g., "SAU").
/// Aggregate root for the country bounded context. Soft-deletable. <see cref="IsActive"/>
/// hides a country from public dropdowns without deleting historical references.
/// </summary>
[Audited]
public sealed class Country : AggregateRoot<System.Guid>
{
    private static readonly Regex Alpha3Pattern = new("^[A-Z]{3}$", RegexOptions.Compiled);
    private static readonly Regex Alpha2Pattern = new("^[A-Z]{2}$", RegexOptions.Compiled);

    private Country(
        System.Guid id,
        string isoAlpha3,
        string isoAlpha2,
        string nameAr,
        string nameEn,
        string regionAr,
        string regionEn,
        string flagUrl) : base(id)
    {
        IsoAlpha3 = isoAlpha3;
        IsoAlpha2 = isoAlpha2;
        NameAr = nameAr;
        NameEn = nameEn;
        RegionAr = regionAr;
        RegionEn = regionEn;
        FlagUrl = flagUrl;
        IsActive = true;
    }

    public string IsoAlpha3 { get; private set; }
    public string IsoAlpha2 { get; private set; }
    public string NameAr { get; private set; }
    public string NameEn { get; private set; }
    public string RegionAr { get; private set; }
    public string RegionEn { get; private set; }
    public string FlagUrl { get; private set; }
    public System.Guid? LatestKapsarcSnapshotId { get; private set; }
    public bool IsActive { get; private set; }

    public static Country Register(
        string isoAlpha3,
        string isoAlpha2,
        string nameAr,
        string nameEn,
        string regionAr,
        string regionEn,
        string flagUrl)
    {
        if (string.IsNullOrWhiteSpace(isoAlpha3) || !Alpha3Pattern.IsMatch(isoAlpha3))
        {
            throw new DomainException($"IsoAlpha3 '{isoAlpha3}' must be three uppercase letters.");
        }
        if (string.IsNullOrWhiteSpace(isoAlpha2) || !Alpha2Pattern.IsMatch(isoAlpha2))
        {
            throw new DomainException($"IsoAlpha2 '{isoAlpha2}' must be two uppercase letters.");
        }
        if (string.IsNullOrWhiteSpace(nameAr)) throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn)) throw new DomainException("NameEn is required.");
        if (string.IsNullOrWhiteSpace(regionAr)) throw new DomainException("RegionAr is required.");
        if (string.IsNullOrWhiteSpace(regionEn)) throw new DomainException("RegionEn is required.");
        if (string.IsNullOrWhiteSpace(flagUrl)
            || !flagUrl.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("FlagUrl must be https://.");
        }
        return new Country(System.Guid.NewGuid(),
            isoAlpha3, isoAlpha2, nameAr, nameEn, regionAr, regionEn, flagUrl);
    }

    public void UpdateLatestKapsarcSnapshot(System.Guid snapshotId)
    {
        if (snapshotId == System.Guid.Empty)
        {
            throw new DomainException("SnapshotId is required.");
        }
        LatestKapsarcSnapshotId = snapshotId;
    }

    public void UpdateNames(string nameAr, string nameEn, string regionAr, string regionEn)
    {
        if (string.IsNullOrWhiteSpace(nameAr)) throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn)) throw new DomainException("NameEn is required.");
        if (string.IsNullOrWhiteSpace(regionAr)) throw new DomainException("RegionAr is required.");
        if (string.IsNullOrWhiteSpace(regionEn)) throw new DomainException("RegionEn is required.");
        NameAr = nameAr;
        NameEn = nameEn;
        RegionAr = regionAr;
        RegionEn = regionEn;
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
