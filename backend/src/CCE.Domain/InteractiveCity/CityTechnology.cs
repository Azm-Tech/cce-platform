using CCE.Domain.Common;

namespace CCE.Domain.InteractiveCity;

public sealed class CityTechnology : Entity<System.Guid>
{
    private CityTechnology(System.Guid id, string nameAr, string nameEn,
        string descriptionAr, string descriptionEn, string categoryAr, string categoryEn,
        decimal carbonImpactKgPerYear, decimal costUsd, string? iconUrl) : base(id)
    {
        NameAr = nameAr; NameEn = nameEn;
        DescriptionAr = descriptionAr; DescriptionEn = descriptionEn;
        CategoryAr = categoryAr; CategoryEn = categoryEn;
        CarbonImpactKgPerYear = carbonImpactKgPerYear; CostUsd = costUsd;
        IconUrl = iconUrl; IsActive = true;
    }

    public string NameAr { get; private set; }
    public string NameEn { get; private set; }
    public string DescriptionAr { get; private set; }
    public string DescriptionEn { get; private set; }
    public string CategoryAr { get; private set; }
    public string CategoryEn { get; private set; }
    public decimal CarbonImpactKgPerYear { get; private set; }
    public decimal CostUsd { get; private set; }
    public string? IconUrl { get; private set; }
    public bool IsActive { get; private set; }

    public static CityTechnology Create(string nameAr, string nameEn,
        string descriptionAr, string descriptionEn, string categoryAr, string categoryEn,
        decimal carbonImpactKgPerYear, decimal costUsd, string? iconUrl = null)
    {
        if (string.IsNullOrWhiteSpace(nameAr)) throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn)) throw new DomainException("NameEn is required.");
        if (string.IsNullOrWhiteSpace(descriptionAr)) throw new DomainException("DescriptionAr is required.");
        if (string.IsNullOrWhiteSpace(descriptionEn)) throw new DomainException("DescriptionEn is required.");
        if (string.IsNullOrWhiteSpace(categoryAr)) throw new DomainException("CategoryAr is required.");
        if (string.IsNullOrWhiteSpace(categoryEn)) throw new DomainException("CategoryEn is required.");
        if (costUsd < 0) throw new DomainException("CostUsd cannot be negative.");
        if (iconUrl is not null
            && !iconUrl.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
            throw new DomainException("IconUrl must be https://.");
        return new CityTechnology(System.Guid.NewGuid(), nameAr, nameEn,
            descriptionAr, descriptionEn, categoryAr, categoryEn,
            carbonImpactKgPerYear, costUsd, iconUrl);
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public void UpdateImpact(decimal carbonImpactKgPerYear, decimal costUsd)
    {
        if (costUsd < 0) throw new DomainException("CostUsd cannot be negative.");
        CarbonImpactKgPerYear = carbonImpactKgPerYear;
        CostUsd = costUsd;
    }
}
