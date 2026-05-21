using CCE.Domain.Common;

namespace CCE.Domain.PlatformSettings;

[Audited]
public sealed class HomepageSettings : AggregateRoot<System.Guid>
{
    private HomepageSettings(
        System.Guid id,
        string objectiveAr,
        string objectiveEn) : base(id)
    {
        ObjectiveAr = objectiveAr;
        ObjectiveEn = objectiveEn;
    }

    public string? VideoUrl { get; private set; }
    public string ObjectiveAr { get; private set; }
    public string ObjectiveEn { get; private set; }
    public string CceConceptsAr { get; private set; } = string.Empty;
    public string CceConceptsEn { get; private set; } = string.Empty;
    public byte[] RowVersion { get; private set; } = System.Array.Empty<byte>();

    public static HomepageSettings Create(string objectiveAr, string objectiveEn)
    {
        if (string.IsNullOrWhiteSpace(objectiveAr)) throw new DomainException("ObjectiveAr is required.");
        if (string.IsNullOrWhiteSpace(objectiveEn)) throw new DomainException("ObjectiveEn is required.");
        return new HomepageSettings(System.Guid.NewGuid(), objectiveAr, objectiveEn);
    }

    public void UpdateContent(
        string? videoUrl,
        string objectiveAr,
        string objectiveEn,
        string cceConceptsAr,
        string cceConceptsEn)
    {
        if (string.IsNullOrWhiteSpace(objectiveAr)) throw new DomainException("ObjectiveAr is required.");
        if (string.IsNullOrWhiteSpace(objectiveEn)) throw new DomainException("ObjectiveEn is required.");
        VideoUrl = videoUrl;
        ObjectiveAr = objectiveAr;
        ObjectiveEn = objectiveEn;
        CceConceptsAr = cceConceptsAr ?? string.Empty;
        CceConceptsEn = cceConceptsEn ?? string.Empty;
    }
}
