using CCE.Application.Common.Interfaces;
using CCE.Application.KnowledgeMaps.Public.Queries.ListKnowledgeMaps;
using CCE.Domain.KnowledgeMaps;

namespace CCE.Application.Tests.KnowledgeMaps.Public.Queries;

public class ListKnowledgeMapsQueryHandlerTests
{
    [Fact]
    public async Task Returns_empty_list_when_no_maps_exist()
    {
        var db = BuildDb(System.Array.Empty<KnowledgeMap>());
        var sut = new ListKnowledgeMapsQueryHandler(db);

        var result = await sut.Handle(new ListKnowledgeMapsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_maps_sorted_by_NameEn()
    {
        var mapA = KnowledgeMap.Create("ب", "Bravo", "وصف", "desc", "bravo");
        var mapB = KnowledgeMap.Create("أ", "Alpha", "وصف", "desc", "alpha");

        var db = BuildDb(new[] { mapA, mapB });
        var sut = new ListKnowledgeMapsQueryHandler(db);

        var result = await sut.Handle(new ListKnowledgeMapsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].NameEn.Should().Be("Alpha");
        result[1].NameEn.Should().Be("Bravo");
    }

    private static ICceDbContext BuildDb(IEnumerable<KnowledgeMap> maps)
    {
        var db = Substitute.For<ICceDbContext>();
        db.KnowledgeMaps.Returns(maps.AsQueryable());
        return db;
    }
}
