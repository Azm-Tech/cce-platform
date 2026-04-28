using CCE.Domain.KnowledgeMaps;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CCE.Seeder.Seeders;

public sealed class KnowledgeMapSeeder : ISeeder
{
    private readonly CceDbContext _ctx;
    private readonly ILogger<KnowledgeMapSeeder> _logger;

    public KnowledgeMapSeeder(CceDbContext ctx, ILogger<KnowledgeMapSeeder> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }

    public int Order => 30;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var mapId = DeterministicGuid.From("knowledge_map:cce-basics");
        var mapExists = await _ctx.KnowledgeMaps.IgnoreQueryFilters()
            .AnyAsync(m => m.Id == mapId, cancellationToken).ConfigureAwait(false);
        if (!mapExists)
        {
            var map = KnowledgeMap.Create(
                "أساسيات الاقتصاد الكربوني الدائري",
                "Circular Carbon Economy Basics",
                "خريطة معرفية تشرح المبادئ الأساسية",
                "Knowledge map of the four R's", "cce-basics");
            typeof(KnowledgeMap).GetProperty(nameof(map.Id))!.SetValue(map, mapId);
            _ctx.KnowledgeMaps.Add(map);
        }

        var nodes = new[]
        {
            ("reduce", "تقليل", "Reduce", NodeType.Sector, 100.0, 100.0, 0),
            ("reuse", "إعادة استخدام", "Reuse", NodeType.Sector, 300.0, 100.0, 1),
            ("recycle", "إعادة تدوير", "Recycle", NodeType.Sector, 100.0, 300.0, 2),
            ("remove", "إزالة الكربون", "Remove", NodeType.Sector, 300.0, 300.0, 3),
        };

        var nodeIds = new Dictionary<string, System.Guid>();
        foreach (var (slug, nameAr, nameEn, type, x, y, order) in nodes)
        {
            var id = DeterministicGuid.From($"km_node:cce-basics:{slug}");
            nodeIds[slug] = id;
            var exists = await _ctx.KnowledgeMapNodes
                .AnyAsync(n => n.Id == id, cancellationToken).ConfigureAwait(false);
            if (exists) continue;
            var node = KnowledgeMapNode.Create(mapId, nameAr, nameEn, type, null, null, null, x, y, order);
            typeof(KnowledgeMapNode).GetProperty(nameof(node.Id))!.SetValue(node, id);
            _ctx.KnowledgeMapNodes.Add(node);
        }

        var edges = new[]
        {
            ("reduce", "reuse", RelationshipType.RelatedTo),
            ("reuse", "recycle", RelationshipType.RelatedTo),
            ("recycle", "remove", RelationshipType.RelatedTo),
        };

        for (var i = 0; i < edges.Length; i++)
        {
            var (from, to, rel) = edges[i];
            var id = DeterministicGuid.From($"km_edge:cce-basics:{from}-{to}-{rel}");
            var exists = await _ctx.KnowledgeMapEdges
                .AnyAsync(e => e.Id == id, cancellationToken).ConfigureAwait(false);
            if (exists) continue;
            var edge = KnowledgeMapEdge.Connect(mapId, nodeIds[from], nodeIds[to], rel, i);
            typeof(KnowledgeMapEdge).GetProperty(nameof(edge.Id))!.SetValue(edge, id);
            _ctx.KnowledgeMapEdges.Add(edge);
        }

        await _ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
