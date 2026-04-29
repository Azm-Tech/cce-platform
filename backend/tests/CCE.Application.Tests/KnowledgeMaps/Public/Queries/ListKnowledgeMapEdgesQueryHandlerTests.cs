using CCE.Application.Common.Interfaces;
using CCE.Application.KnowledgeMaps.Public.Queries.ListKnowledgeMapEdges;
using CCE.Domain.KnowledgeMaps;

namespace CCE.Application.Tests.KnowledgeMaps.Public.Queries;

public class ListKnowledgeMapEdgesQueryHandlerTests
{
    [Fact]
    public async Task Returns_empty_list_when_map_has_no_edges()
    {
        var mapId = System.Guid.NewGuid();
        var db = BuildDb(System.Array.Empty<KnowledgeMapEdge>());
        var sut = new ListKnowledgeMapEdgesQueryHandler(db);

        var result = await sut.Handle(new ListKnowledgeMapEdgesQuery(mapId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_only_edges_belonging_to_requested_map()
    {
        var mapId = System.Guid.NewGuid();
        var otherId = System.Guid.NewGuid();
        var fromNode = System.Guid.NewGuid();
        var toNode = System.Guid.NewGuid();

        var edgeInMap = KnowledgeMapEdge.Connect(mapId, fromNode, toNode, RelationshipType.ParentOf, 0);
        var edgeOther = KnowledgeMapEdge.Connect(otherId, fromNode, toNode, RelationshipType.RelatedTo, 1);

        var db = BuildDb(new[] { edgeInMap, edgeOther });
        var sut = new ListKnowledgeMapEdgesQueryHandler(db);

        var result = await sut.Handle(new ListKnowledgeMapEdgesQuery(mapId), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].MapId.Should().Be(mapId);
        result[0].RelationshipType.Should().Be(RelationshipType.ParentOf);
    }

    private static ICceDbContext BuildDb(IEnumerable<KnowledgeMapEdge> edges)
    {
        var db = Substitute.For<ICceDbContext>();
        db.KnowledgeMapEdges.Returns(edges.AsQueryable());
        return db;
    }
}
