using CCE.Domain.Common;

namespace CCE.Domain.PlatformSettings;

public sealed class GlossaryEntry : AggregateRoot<System.Guid>
{
    private GlossaryEntry(
        System.Guid id,
        System.Guid aboutSettingsId,
        string termAr,
        string termEn,
        string definitionAr,
        string definitionEn,
        int orderIndex) : base(id)
    {
        AboutSettingsId = aboutSettingsId;
        TermAr = termAr;
        TermEn = termEn;
        DefinitionAr = definitionAr;
        DefinitionEn = definitionEn;
        OrderIndex = orderIndex;
    }

    public System.Guid AboutSettingsId { get; private set; }
    public string TermAr { get; private set; }
    public string TermEn { get; private set; }
    public string DefinitionAr { get; private set; }
    public string DefinitionEn { get; private set; }
    public int OrderIndex { get; private set; }

    public static GlossaryEntry Create(
        System.Guid aboutSettingsId,
        string termAr,
        string termEn,
        string definitionAr,
        string definitionEn,
        int orderIndex)
    {
        if (aboutSettingsId == System.Guid.Empty)
            throw new DomainException("AboutSettingsId is required.");
        if (string.IsNullOrWhiteSpace(termAr))
            throw new DomainException("TermAr is required.");
        if (string.IsNullOrWhiteSpace(termEn))
            throw new DomainException("TermEn is required.");
        if (string.IsNullOrWhiteSpace(definitionAr))
            throw new DomainException("DefinitionAr is required.");
        if (string.IsNullOrWhiteSpace(definitionEn))
            throw new DomainException("DefinitionEn is required.");
        return new GlossaryEntry(
            System.Guid.NewGuid(), aboutSettingsId,
            termAr, termEn, definitionAr, definitionEn, orderIndex);
    }

    public void UpdateContent(
        string termAr,
        string termEn,
        string definitionAr,
        string definitionEn)
    {
        if (string.IsNullOrWhiteSpace(termAr))
            throw new DomainException("TermAr is required.");
        if (string.IsNullOrWhiteSpace(termEn))
            throw new DomainException("TermEn is required.");
        if (string.IsNullOrWhiteSpace(definitionAr))
            throw new DomainException("DefinitionAr is required.");
        if (string.IsNullOrWhiteSpace(definitionEn))
            throw new DomainException("DefinitionEn is required.");
        TermAr = termAr;
        TermEn = termEn;
        DefinitionAr = definitionAr;
        DefinitionEn = definitionEn;
    }

    public void Reorder(int orderIndex) => OrderIndex = orderIndex;
}
