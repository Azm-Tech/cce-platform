using CCE.Domain.Common;
using CCE.Domain.PlatformSettings.ValueObjects;

namespace CCE.Domain.PlatformSettings;

[Audited]
public sealed class HomepageSettings : AggregateRoot<System.Guid>
{
    private HomepageSettings() : base(System.Guid.Empty) { } // EF Core materialization

    private HomepageSettings(System.Guid id, LocalizedText objective) : base(id)
    {
        Objective = objective;
    }

    public string? VideoUrl { get; private set; }
    public LocalizedText Objective { get; private set; } = null!;
    public string CceConceptsAr { get; private set; } = string.Empty;
    public string CceConceptsEn { get; private set; } = string.Empty;
    public byte[] RowVersion { get; private set; } = System.Array.Empty<byte>();

    public System.Collections.Generic.ICollection<HomepageCountry> Countries { get; private set; } = [];

    public static HomepageSettings Create(LocalizedText objective, System.Guid by, ISystemClock clock)
    {
        var settings = new HomepageSettings(System.Guid.NewGuid(), objective);
        settings.MarkAsCreated(by, clock);
        return settings;
    }

    public void UpdateContent(
        string? videoUrl,
        LocalizedText objective,
        string cceConceptsAr,
        string cceConceptsEn,
        System.Guid by,
        ISystemClock clock)
    {
        VideoUrl = videoUrl;
        Objective = objective;
        CceConceptsAr = cceConceptsAr ?? string.Empty;
        CceConceptsEn = cceConceptsEn ?? string.Empty;
        MarkAsModified(by, clock);
    }

    public void SyncCountries(System.Collections.Generic.IEnumerable<System.Guid> countryIds, System.Guid by, ISystemClock clock)
    {
        var incoming = countryIds.ToList();
        var existing = Countries.ToList();

        // Remove countries not in the incoming list
        foreach (var ec in existing.Where(e => !incoming.Contains(e.CountryId)).ToList())
        {
            Countries.Remove(ec);
        }

        // Re-order / add new
        var existingById = existing.ToDictionary(e => e.CountryId);
        for (int i = 0; i < incoming.Count; i++)
        {
            if (existingById.TryGetValue(incoming[i], out var country))
            {
                country.Reorder(i);
            }
            else
            {
                Countries.Add(HomepageCountry.Create(Id, incoming[i], i, by, clock));
            }
        }
    }
}
