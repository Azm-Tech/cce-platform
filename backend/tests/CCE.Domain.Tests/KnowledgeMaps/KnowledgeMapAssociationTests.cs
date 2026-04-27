using CCE.Domain.Common;
using CCE.Domain.KnowledgeMaps;

namespace CCE.Domain.Tests.KnowledgeMaps;

public class KnowledgeMapAssociationTests
{
    [Fact]
    public void Associate_to_resource() {
        var a = KnowledgeMapAssociation.Associate(
            System.Guid.NewGuid(), AssociatedType.Resource, System.Guid.NewGuid());
        a.AssociatedType.Should().Be(AssociatedType.Resource);
    }

    [Fact]
    public void Empty_nodeId_throws() {
        var act = () => KnowledgeMapAssociation.Associate(
            System.Guid.Empty, AssociatedType.Event, System.Guid.NewGuid());
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Empty_associatedId_throws() {
        var act = () => KnowledgeMapAssociation.Associate(
            System.Guid.NewGuid(), AssociatedType.News, System.Guid.Empty);
        act.Should().Throw<DomainException>();
    }
}
