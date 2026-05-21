using CCE.Domain.Common;

namespace CCE.Domain.PlatformSettings;

public sealed class HomepageCountry : Entity<System.Guid>
{
    private HomepageCountry(System.Guid id, System.Guid homepageSettingsId, System.Guid countryId, int orderIndex)
        : base(id)
    {
        HomepageSettingsId = homepageSettingsId;
        CountryId = countryId;
        OrderIndex = orderIndex;
    }

    public System.Guid HomepageSettingsId { get; private set; }
    public System.Guid CountryId { get; private set; }
    public int OrderIndex { get; private set; }

    public static HomepageCountry Create(System.Guid homepageSettingsId, System.Guid countryId, int orderIndex = 0)
    {
        if (homepageSettingsId == System.Guid.Empty)
            throw new DomainException("HomepageSettingsId is required.");
        if (countryId == System.Guid.Empty)
            throw new DomainException("CountryId is required.");
        return new HomepageCountry(System.Guid.NewGuid(), homepageSettingsId, countryId, orderIndex);
    }
}
