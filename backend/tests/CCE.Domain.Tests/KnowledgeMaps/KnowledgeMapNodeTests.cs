using CCE.Domain.Common;
using CCE.Domain.KnowledgeMaps;

namespace CCE.Domain.Tests.KnowledgeMaps;

public class KnowledgeMapNodeTests
{
    private static KnowledgeMapNode NewNode() => KnowledgeMapNode.Create(
        System.Guid.NewGuid(), "تقنية", "Tech", NodeType.Technology, null, null,
        null, 100, 200, 0);

    [Fact]
    public void Create_node() { NewNode().NodeType.Should().Be(NodeType.Technology); }

    [Fact]
    public void Empty_mapId_throws() {
        var act = () => KnowledgeMapNode.Create(System.Guid.Empty, "ا", "x",
            NodeType.Sector, null, null, null, 0, 0, 0);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void IconUrl_must_be_https() {
        var act = () => KnowledgeMapNode.Create(System.Guid.NewGuid(), "ا", "x",
            NodeType.Sector, null, null, "http://insecure", 0, 0, 0);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateLayout_changes_coordinates() {
        var n = NewNode();
        n.UpdateLayout(500, 600);
        n.LayoutX.Should().Be(500);
        n.LayoutY.Should().Be(600);
    }

    [Fact]
    public void Reorder_updates_OrderIndex() {
        var n = NewNode();
        n.Reorder(7);
        n.OrderIndex.Should().Be(7);
    }
}
