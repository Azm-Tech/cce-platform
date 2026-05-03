using System.Runtime.CompilerServices;
using CCE.Domain.Content;
using CCE.Domain.KnowledgeMaps;
using CCE.Infrastructure.Assistant;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Application.Tests.Assistant;

public sealed class CitationSearchTests : IDisposable
{
    private readonly CceDbContext _db;
    private readonly CitationSearch _sut;

    public CitationSearchTests()
    {
        var opts = new DbContextOptionsBuilder<CceDbContext>()
            .UseInMemoryDatabase($"citation-search-{Guid.NewGuid()}")
            .Options;
        _db = new CceDbContext(opts);
        _sut = new CitationSearch(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Returns_empty_when_no_query_tokens()
    {
        var result = await _sut.FindCitationsAsync("", "", "en", default);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_empty_when_no_rows_match()
    {
        var result = await _sut.FindCitationsAsync("solar panels", "", "en", default);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Picks_resource_with_highest_token_overlap()
    {
        SeedResource(Guid.NewGuid(), "Solar Panel Installation Guide", "دليل تركيب الألواح الشمسية");
        SeedResource(Guid.NewGuid(), "Wind Turbine Maintenance", "صيانة توربينات الرياح");
        await _db.SaveChangesAsync();

        var result = await _sut.FindCitationsAsync("How do I install solar panels?", "", "en", default);

        result.Should().ContainSingle(c => c.Kind == "resource");
        result.Single(c => c.Kind == "resource").Title.Should().Contain("Solar");
    }

    [Fact]
    public async Task Picks_map_node_with_highest_token_overlap()
    {
        var mapId = Guid.NewGuid();
        SeedMapNode(KnowledgeMapNode.Create(mapId, "احتجاز الكربون", "Carbon Capture",
            NodeType.Sector, null, null, null, 0, 0, 0));
        SeedMapNode(KnowledgeMapNode.Create(mapId, "طاقة متجددة", "Renewable Energy",
            NodeType.Sector, null, null, null, 0, 0, 1));
        await _db.SaveChangesAsync();

        var result = await _sut.FindCitationsAsync("Tell me about carbon capture", "", "en", default);

        result.Should().ContainSingle(c => c.Kind == "map-node");
        result.Single(c => c.Kind == "map-node").Title.Should().Be("Carbon Capture");
    }

    [Fact]
    public async Task Locale_ar_uses_Arabic_title_fields()
    {
        var mapId = Guid.NewGuid();
        SeedMapNode(KnowledgeMapNode.Create(mapId, "احتجاز الكربون", "Carbon Capture",
            NodeType.Sector, null, null, null, 0, 0, 0));
        await _db.SaveChangesAsync();

        var result = await _sut.FindCitationsAsync("احتجاز الكربون", "", "ar", default);

        result.Single(c => c.Kind == "map-node").Title.Should().Be("احتجاز الكربون");
    }

    [Fact]
    public async Task Returns_at_most_one_of_each_kind()
    {
        var mapId = Guid.NewGuid();
        for (int i = 0; i < 5; i++)
        {
            SeedResource(Guid.NewGuid(), $"Solar Resource {i}", $"مورد شمسي {i}");
            SeedMapNode(KnowledgeMapNode.Create(mapId, $"Solar Node {i}", $"Solar Node {i}",
                NodeType.Sector, null, null, null, 0, 0, i));
        }
        await _db.SaveChangesAsync();

        var result = await _sut.FindCitationsAsync("solar", "", "en", default);
        result.Count(c => c.Kind == "resource").Should().BeLessThanOrEqualTo(1);
        result.Count(c => c.Kind == "map-node").Should().BeLessThanOrEqualTo(1);
    }

    private void SeedResource(Guid id, string titleEn, string titleAr)
    {
        // The Resource aggregate has a Draft factory that requires many FK
        // GUIDs; for tokenizer-only tests we bypass the factory and set
        // just the fields the CitationSearch query projects.
        var resource = (Resource)RuntimeHelpers.GetUninitializedObject(typeof(Resource));
        SetProp(resource, nameof(Resource.Id), id);
        SetProp(resource, nameof(Resource.TitleEn), titleEn);
        SetProp(resource, nameof(Resource.TitleAr), titleAr);
        SetProp(resource, nameof(Resource.DescriptionEn), string.Empty);
        SetProp(resource, nameof(Resource.DescriptionAr), string.Empty);
        SetProp(resource, nameof(Resource.RowVersion), Array.Empty<byte>());
        _db.Resources.Add(resource);
    }

    private void SeedMapNode(KnowledgeMapNode node)
    {
        _db.KnowledgeMapNodes.Add(node);
    }

    private static void SetProp<T>(T obj, string name, object value)
    {
        typeof(T).GetProperty(name)!.SetValue(obj, value);
    }
}
