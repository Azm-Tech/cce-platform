using CCE.Domain.Common;

namespace CCE.Domain.KnowledgeMaps;

public sealed class KnowledgeMapNode : Entity<System.Guid>
{
    private KnowledgeMapNode(System.Guid id, System.Guid mapId, string nameAr, string nameEn,
        NodeType nodeType, string? descriptionAr, string? descriptionEn,
        string? iconUrl, double layoutX, double layoutY, int orderIndex) : base(id)
    {
        MapId = mapId; NameAr = nameAr; NameEn = nameEn;
        NodeType = nodeType; DescriptionAr = descriptionAr; DescriptionEn = descriptionEn;
        IconUrl = iconUrl; LayoutX = layoutX; LayoutY = layoutY; OrderIndex = orderIndex;
    }

    public System.Guid MapId { get; private set; }
    public string NameAr { get; private set; }
    public string NameEn { get; private set; }
    public NodeType NodeType { get; private set; }
    public string? DescriptionAr { get; private set; }
    public string? DescriptionEn { get; private set; }
    public string? IconUrl { get; private set; }
    public double LayoutX { get; private set; }
    public double LayoutY { get; private set; }
    public int OrderIndex { get; private set; }

    public static KnowledgeMapNode Create(System.Guid mapId, string nameAr, string nameEn,
        NodeType nodeType, string? descriptionAr, string? descriptionEn,
        string? iconUrl, double layoutX, double layoutY, int orderIndex)
    {
        if (mapId == System.Guid.Empty) throw new DomainException("MapId is required.");
        if (string.IsNullOrWhiteSpace(nameAr)) throw new DomainException("NameAr is required.");
        if (string.IsNullOrWhiteSpace(nameEn)) throw new DomainException("NameEn is required.");
        if (iconUrl is not null
            && !iconUrl.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
            throw new DomainException("IconUrl must be https://.");
        return new KnowledgeMapNode(System.Guid.NewGuid(), mapId, nameAr, nameEn,
            nodeType, descriptionAr, descriptionEn, iconUrl, layoutX, layoutY, orderIndex);
    }

    public void UpdateLayout(double x, double y) { LayoutX = x; LayoutY = y; }
    public void Reorder(int orderIndex) => OrderIndex = orderIndex;
}
