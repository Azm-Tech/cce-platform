using CCE.Domain.Common;

namespace CCE.Domain.InteractiveCity;

[Audited]
public sealed class CityScenario : AggregateRoot<System.Guid>, ISoftDeletable
{
    public const int MinTargetYear = 2030;
    public const int MaxTargetYear = 2080;

    private CityScenario(System.Guid id, System.Guid userId, string nameAr, string nameEn,
        CityType cityType, int targetYear, string configurationJson,
        System.DateTimeOffset createdOn) : base(id)
    {
        UserId = userId; NameAr = nameAr; NameEn = nameEn;
        CityType = cityType; TargetYear = targetYear;
        ConfigurationJson = configurationJson;
        CreatedOn = createdOn; LastModifiedOn = createdOn;
    }

    public System.Guid UserId { get; private set; }
    public string NameAr { get; private set; }
    public string NameEn { get; private set; }
    public CityType CityType { get; private set; }
    public int TargetYear { get; private set; }
    public string ConfigurationJson { get; private set; }
    public System.DateTimeOffset CreatedOn { get; private set; }
    public System.DateTimeOffset LastModifiedOn { get; private set; }
    public bool IsDeleted { get; private set; }
    public System.DateTimeOffset? DeletedOn { get; private set; }
    public System.Guid? DeletedById { get; private set; }

    public static CityScenario Create(System.Guid userId, string nameAr, string nameEn,
        CityType cityType, int targetYear, string configurationJson, ISystemClock clock)
    {
        if (userId == System.Guid.Empty) throw new DomainException("UserId is required.");
        if (string.IsNullOrWhiteSpace(nameAr)) throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn)) throw new DomainException("NameEn is required.");
        if (targetYear < MinTargetYear || targetYear > MaxTargetYear)
            throw new DomainException($"TargetYear must be between {MinTargetYear} and {MaxTargetYear}.");
        if (string.IsNullOrWhiteSpace(configurationJson))
            throw new DomainException("ConfigurationJson is required.");
        return new CityScenario(System.Guid.NewGuid(), userId, nameAr, nameEn,
            cityType, targetYear, configurationJson, clock.UtcNow);
    }

    public void UpdateConfiguration(string configurationJson, ISystemClock clock)
    {
        if (string.IsNullOrWhiteSpace(configurationJson))
            throw new DomainException("ConfigurationJson is required.");
        ConfigurationJson = configurationJson;
        LastModifiedOn = clock.UtcNow;
    }

    public void Rename(string nameAr, string nameEn, ISystemClock clock)
    {
        if (string.IsNullOrWhiteSpace(nameAr)) throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn)) throw new DomainException("NameEn is required.");
        NameAr = nameAr; NameEn = nameEn;
        LastModifiedOn = clock.UtcNow;
    }

    public void SoftDelete(System.Guid deletedById, ISystemClock clock)
    {
        if (deletedById == System.Guid.Empty) throw new DomainException("DeletedById is required.");
        if (IsDeleted) return;
        IsDeleted = true;
        DeletedById = deletedById;
        DeletedOn = clock.UtcNow;
    }
}
