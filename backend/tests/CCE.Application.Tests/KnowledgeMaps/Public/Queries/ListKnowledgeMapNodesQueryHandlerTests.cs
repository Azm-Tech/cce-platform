using CCE.Application.Common.Interfaces;
using CCE.Application.KnowledgeMaps.Public.Queries.ListKnowledgeMapNodes;
using CCE.Domain.KnowledgeMaps;

namespace CCE.Application.Tests.KnowledgeMaps.Public.Queries;

public class ListKnowledgeMapNodesQueryHandlerTests
{
    [Fact]
    public async Task Returns_empty_list_when_map_has_no_nodes()
    {
        var mapId = System.Guid.NewGuid();
        var db = BuildDb(System.Array.Empty<KnowledgeMapNode>());
        var sut = new ListKnowledgeMapNodesQueryHandler(db);

        var result = await sut.Handle(new ListKnowledgeMapNodesQuery(mapId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_only_nodes_belonging_to_requested_map()
    {
        var mapId = System.Guid.NewGuid();
        var otherId = System.Guid.NewGuid();

        var nodeInMap = KnowledgeMapNode.Create(mapId, "عقدة", "Node A", NodeType.Technology,
            null, null, null, 0.0, 0.0, 0);
        var nodeOther = KnowledgeMapNode.Create(otherId, "عقدة", "Node B", NodeType.Sector,
            null, null, null, 1.0, 1.0, 1);

        var db = BuildDb(new[] { nodeInMap, nodeOther });
        var sut = new ListKnowledgeMapNodesQueryHandler(db);

        var result = await sut.Handle(new ListKnowledgeMapNodesQuery(mapId), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].NameEn.Should().Be("Node A");
        result[0].MapId.Should().Be(mapId);
    }

    private static ICceDbContext BuildDb(IEnumerable<KnowledgeMapNode> nodes)
    {
        var db = Substitute.For<ICceDbContext>();
        db.KnowledgeMapNodes.Returns(nodes.AsQueryable());
        return db;
    }
}
