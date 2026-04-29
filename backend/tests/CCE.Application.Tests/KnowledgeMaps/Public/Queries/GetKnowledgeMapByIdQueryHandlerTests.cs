using CCE.Application.Common.Interfaces;
using CCE.Application.KnowledgeMaps.Public.Queries.GetKnowledgeMapById;
using CCE.Domain.KnowledgeMaps;

namespace CCE.Application.Tests.KnowledgeMaps.Public.Queries;

public class GetKnowledgeMapByIdQueryHandlerTests
{
    [Fact]
    public async Task Returns_dto_when_map_found()
    {
        var map = KnowledgeMap.Create("خريطة", "Energy Map", "وصف", "Description", "energy-map");

        var db = BuildDb(new[] { map });
        var sut = new GetKnowledgeMapByIdQueryHandler(db);

        var result = await sut.Handle(new GetKnowledgeMapByIdQuery(map.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.NameEn.Should().Be("Energy Map");
        result.Slug.Should().Be("energy-map");
    }

    [Fact]
    public async Task Returns_null_when_map_not_found()
    {
        var db = BuildDb(System.Array.Empty<KnowledgeMap>());
        var sut = new GetKnowledgeMapByIdQueryHandler(db);

        var result = await sut.Handle(new GetKnowledgeMapByIdQuery(System.Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    private static ICceDbContext BuildDb(IEnumerable<KnowledgeMap> maps)
    {
        var db = Substitute.For<ICceDbContext>();
        db.KnowledgeMaps.Returns(maps.AsQueryable());
        return db;
    }
}
