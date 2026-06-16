using CCE.Domain.Common;
using CCE.Domain.Content;

namespace CCE.Domain.InteractiveMaps;

[Audited]
public sealed class InteractiveMapNode : Entity<System.Guid>
{
    private readonly List<Tag> _tags = new();

    private InteractiveMapNode(
        System.Guid id,
        System.Guid interactiveMapId,
        string nameAr,
        string nameEn,
        string iconKey,
        int? category,
        string? categoryNameAr,
        string? categoryNameEn,
        int level,
        System.Guid? parentId,
        System.Guid topicId) : base(id)
    {
        InteractiveMapId = interactiveMapId;
        NameAr = nameAr;
        NameEn = nameEn;
        IconKey = iconKey;
        Category = category;
        CategoryNameAr = categoryNameAr;
        CategoryNameEn = categoryNameEn;
        Level = level;
        ParentId = parentId;
        TopicId = topicId;
        IsActive = true;
    }

    public System.Guid InteractiveMapId { get; private set; }

    public string NameAr { get; private set; }

    public string NameEn { get; private set; }

    public string IconKey { get; private set; }

    public int? Category { get; private set; }

    public string? CategoryNameAr { get; private set; }

    public string? CategoryNameEn { get; private set; }

    public int Level { get; private set; }

    public System.Guid? ParentId { get; private set; }

    public System.Guid TopicId { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<Tag> Tags => _tags.AsReadOnly();

    public static InteractiveMapNode Create(
        System.Guid interactiveMapId,
        string nameAr,
        string nameEn,
        string iconKey,
        int? category,
        string? categoryNameAr,
        string? categoryNameEn,
        int level,
        System.Guid? parentId,
        System.Guid topicId)
    {
        if (string.IsNullOrWhiteSpace(nameAr))
            throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn))
            throw new DomainException("NameEn is required.");
        if (string.IsNullOrWhiteSpace(iconKey))
            throw new DomainException("IconKey is required.");

        return new InteractiveMapNode(
            id: System.Guid.NewGuid(),
            interactiveMapId: interactiveMapId,
            nameAr: nameAr,
            nameEn: nameEn,
            iconKey: iconKey,
            category: category,
            categoryNameAr: categoryNameAr,
            categoryNameEn: categoryNameEn,
            level: level,
            parentId: parentId,
            topicId: topicId);
    }

    public void UpdateDetails(
        string nameAr,
        string nameEn,
        string iconKey,
        int? category,
        string? categoryNameAr,
        string? categoryNameEn,
        int level,
        System.Guid? parentId,
        System.Guid topicId)
    {
        if (string.IsNullOrWhiteSpace(nameAr))
            throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn))
            throw new DomainException("NameEn is required.");
        if (string.IsNullOrWhiteSpace(iconKey))
            throw new DomainException("IconKey is required.");

        NameAr = nameAr;
        NameEn = nameEn;
        IconKey = iconKey;
        Category = category;
        CategoryNameAr = categoryNameAr;
        CategoryNameEn = categoryNameEn;
        Level = level;
        ParentId = parentId;
        TopicId = topicId;
    }

    public void SetTags(IEnumerable<Tag> tags)
    {
        _tags.Clear();
        _tags.AddRange(tags);
    }

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;
}
