using CCE.Domain.Common;
using CCE.Domain.PlatformSettings.ValueObjects;

namespace CCE.Domain.PlatformSettings;

public sealed class Faq : AuditableEntity<System.Guid>
{
    private Faq() : base(System.Guid.Empty) { }

    private Faq(
        System.Guid id,
        LocalizedText question,
        LocalizedText answer,
        int order) : base(id)
    {
        Question = question;
        Answer = answer;
        Order = order;
    }

    public LocalizedText Question { get; private set; } = null!;
    public LocalizedText Answer { get; private set; } = null!;
    public int Order { get; private set; }

    public static Faq Create(
        LocalizedText question,
        LocalizedText answer,
        int order,
        System.Guid by,
        ISystemClock clock)
    {
        var faq = new Faq(System.Guid.NewGuid(), question, answer, order);
        faq.MarkAsCreated(by, clock);
        return faq;
    }

    public void UpdateContent(
        LocalizedText question,
        LocalizedText answer,
        int order,
        System.Guid by,
        ISystemClock clock)
    {
        Question = question;
        Answer = answer;
        Order = order;
        MarkAsModified(by, clock);
    }
}
