using CCE.Domain.Common;
using CCE.Domain.PlatformSettings.ValueObjects;

namespace CCE.Domain.Lookups;

[Audited]
public sealed class CountryCode : AggregateRoot<System.Guid>
{
    private CountryCode() : base(System.Guid.Empty) { } // EF Core materialization

    private CountryCode(System.Guid id, LocalizedText name, string dialCode) : base(id)
    {
        Name = name;
        DialCode = dialCode;
    }

    public LocalizedText Name { get; private set; } = null!;
    public string DialCode { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    public static CountryCode Create(LocalizedText name, string dialCode, System.Guid by, ISystemClock clock)
    {
        var entity = new CountryCode(System.Guid.NewGuid(), name, dialCode);
        entity.MarkAsCreated(by, clock);
        return entity;
    }

    public void Update(LocalizedText name, string dialCode, bool isActive, System.Guid by, ISystemClock clock)
    {
        Name = name;
        DialCode = dialCode;
        IsActive = isActive;
        MarkAsModified(by, clock);
    }
}
