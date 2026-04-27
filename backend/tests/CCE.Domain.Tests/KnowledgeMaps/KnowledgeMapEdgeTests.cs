using CCE.Domain.Common;
using CCE.Domain.KnowledgeMaps;

namespace CCE.Domain.Tests.KnowledgeMaps;

public class KnowledgeMapEdgeTests
{
    [Fact]
    public void Connect_creates_edge() {
        var e = KnowledgeMapEdge.Connect(
            System.Guid.NewGuid(), System.Guid.NewGuid(), System.Guid.NewGuid(),
            RelationshipType.RelatedTo);
        e.RelationshipType.Should().Be(RelationshipType.RelatedTo);
    }

    [Fact]
    public void Self_loop_throws() {
        var node = System.Guid.NewGuid();
        var act = () => KnowledgeMapEdge.Connect(System.Guid.NewGuid(), node, node, RelationshipType.ParentOf);
        act.Should().Throw<DomainException>().WithMessage("*Self-loop*");
    }

    [Fact]
    public void Empty_mapId_throws() {
        var act = () => KnowledgeMapEdge.Connect(System.Guid.Empty,
            System.Guid.NewGuid(), System.Guid.NewGuid(), RelationshipType.ParentOf);
        act.Should().Throw<DomainException>();
    }
}
