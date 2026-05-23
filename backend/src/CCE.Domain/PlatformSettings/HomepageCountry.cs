using CCE.Domain.Common;

namespace CCE.Domain.PlatformSettings;

public sealed class HomepageCountry : AuditableEntity<System.Guid>
{
    private HomepageCountry() : base(System.Guid.Empty) { } // EF Core materialization

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

    public static HomepageCountry Create(System.Guid homepageSettingsId, System.Guid countryId, int orderIndex, System.Guid by, ISystemClock clock)
    {
        if (homepageSettingsId == System.Guid.Empty)
            throw new DomainException("HomepageSettingsId is required.");
        if (countryId == System.Guid.Empty)
            throw new DomainException("CountryId is required.");

        var hc = new HomepageCountry(System.Guid.NewGuid(), homepageSettingsId, countryId, orderIndex);
        hc.MarkAsCreated(by, clock);
        return hc;
    }

    public void Reorder(int orderIndex) => OrderIndex = orderIndex;
}
