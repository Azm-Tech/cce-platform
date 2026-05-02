using CCE.Domain.KnowledgeMaps;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CCE.Seeder.Seeders;

/// <summary>
/// Seeds two knowledge maps with rich node/edge content:
///   1. cce-basics       — the 4 R's hub with one sub-topic / technology under each
///   2. carbon-capture   — capture techniques with downstream technology dependencies
/// Idempotent: existing nodes/edges are recognised by deterministic GUID and skipped,
/// while existing layout positions are refreshed each run so the map looks tidy after
/// re-seeding into a database that previously held a sparser layout.
/// </summary>
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

    private sealed record NodeSpec(
        string Slug,
        string NameAr,
        string NameEn,
        NodeType Type,
        double X,
        double Y,
        int Order);

    private sealed record EdgeSpec(string FromSlug, string ToSlug, RelationshipType Rel);

    private static readonly NodeSpec[] CceBasicsNodes =
    {
        // Central hub
        new("carbon-cycle",      "الدورة الكربونية",      "Carbon Cycle",          NodeType.Sector,    400, 300, 0),

        // The 4 R's (a ring around the hub)
        new("reduce",            "تقليل",                  "Reduce",                NodeType.Sector,    200, 150, 1),
        new("reuse",             "إعادة استخدام",          "Reuse",                 NodeType.Sector,    600, 150, 2),
        new("recycle",           "إعادة تدوير",            "Recycle",               NodeType.Sector,    200, 450, 3),
        new("remove",            "إزالة الكربون",          "Remove",                NodeType.Sector,    600, 450, 4),

        // Reduce sub-topics (left)
        new("reduce-efficiency", "كفاءة الطاقة",            "Energy Efficiency",     NodeType.SubTopic,   60, 80,  5),
        new("reduce-demand",     "خفض الطلب",              "Demand Reduction",      NodeType.SubTopic,   60, 220, 6),

        // Reuse sub-topics (right)
        new("reuse-symbiosis",   "التكافل الصناعي",         "Industrial Symbiosis",  NodeType.SubTopic,  740, 80,  7),
        new("reuse-heat",        "إعادة استخدام الحرارة",    "Process Heat Reuse",    NodeType.SubTopic,  740, 220, 8),

        // Recycle technologies (bottom-left)
        new("recycle-fuels",     "تحويل CO₂ إلى وقود",      "CO₂-to-Fuels",          NodeType.Technology, 60, 380, 9),
        new("recycle-materials", "تحويل CO₂ إلى مواد",       "CO₂-to-Materials",      NodeType.Technology, 60, 520, 10),

        // Remove technologies (bottom-right)
        new("remove-dac",        "الالتقاط المباشر",         "Direct Air Capture",    NodeType.Technology, 740, 380, 11),
        new("remove-beccs",      "BECCS",                  "BECCS",                 NodeType.Technology, 740, 520, 12),
    };

    private static readonly EdgeSpec[] CceBasicsEdges =
    {
        // Hub-and-spoke: Carbon Cycle parents the four R's
        new("carbon-cycle", "reduce",            RelationshipType.ParentOf),
        new("carbon-cycle", "reuse",             RelationshipType.ParentOf),
        new("carbon-cycle", "recycle",           RelationshipType.ParentOf),
        new("carbon-cycle", "remove",            RelationshipType.ParentOf),

        // Each R parents its branches
        new("reduce",       "reduce-efficiency", RelationshipType.ParentOf),
        new("reduce",       "reduce-demand",     RelationshipType.ParentOf),
        new("reuse",        "reuse-symbiosis",   RelationshipType.ParentOf),
        new("reuse",        "reuse-heat",        RelationshipType.ParentOf),
        new("recycle",      "recycle-fuels",     RelationshipType.ParentOf),
        new("recycle",      "recycle-materials", RelationshipType.ParentOf),
        new("remove",       "remove-dac",        RelationshipType.ParentOf),
        new("remove",       "remove-beccs",      RelationshipType.ParentOf),

        // Cross-links — the 4 R's are progressive cousins
        new("reduce",       "reuse",             RelationshipType.RelatedTo),
        new("reuse",        "recycle",           RelationshipType.RelatedTo),
        new("recycle",      "remove",            RelationshipType.RelatedTo),

        // Inter-branch dependencies — one technology requires another
        new("recycle-fuels", "reduce-efficiency", RelationshipType.RequiredBy),
        new("remove-dac",    "recycle-materials", RelationshipType.RelatedTo),
    };

    private static readonly NodeSpec[] CarbonCaptureNodes =
    {
        new("cc-hub",     "الالتقاط الكربوني",       "Carbon Capture",        NodeType.Sector,    400,  80, 0),

        // Capture techniques (middle row)
        new("pre-comb",   "ما قبل الاحتراق",        "Pre-Combustion",        NodeType.SubTopic,  160, 240, 1),
        new("post-comb",  "ما بعد الاحتراق",         "Post-Combustion",       NodeType.SubTopic,  400, 240, 2),
        new("oxy-fuel",   "الوقود الأكسجيني",        "Oxy-Fuel",              NodeType.SubTopic,  640, 240, 3),

        // Technologies that implement those techniques (lower row)
        new("amine",      "محاليل الأمين",           "Amine Solvents",        NodeType.Technology, 160, 400, 4),
        new("membrane",   "أغشية الفصل",             "Membrane Separation",   NodeType.Technology, 400, 400, 5),
        new("mineral",    "التمعدن",                 "Mineralization",        NodeType.Technology, 640, 400, 6),

        // Removal-adjacent technologies (bottom)
        new("dac2",       "الالتقاط المباشر",         "Direct Air Capture",    NodeType.Technology, 240, 540, 7),
        new("ocean",      "الالتقاط المحيطي",         "Ocean Capture",         NodeType.Technology, 560, 540, 8),
    };

    private static readonly EdgeSpec[] CarbonCaptureEdges =
    {
        new("cc-hub",    "pre-comb",  RelationshipType.ParentOf),
        new("cc-hub",    "post-comb", RelationshipType.ParentOf),
        new("cc-hub",    "oxy-fuel",  RelationshipType.ParentOf),

        new("pre-comb",  "membrane",  RelationshipType.RequiredBy),
        new("post-comb", "amine",     RelationshipType.RequiredBy),
        new("oxy-fuel",  "membrane",  RelationshipType.RequiredBy),
        new("post-comb", "mineral",   RelationshipType.RelatedTo),

        new("amine",     "dac2",      RelationshipType.RelatedTo),
        new("mineral",   "ocean",     RelationshipType.RelatedTo),
    };

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedMapAsync(
            slug: "cce-basics",
            nameAr: "أساسيات الاقتصاد الكربوني الدائري",
            nameEn: "Circular Carbon Economy Basics",
            descAr: "خريطة معرفية تشرح المبادئ الأساسية للـ 4Rs",
            descEn: "Knowledge map of the 4 R's of the Circular Carbon Economy",
            nodes: CceBasicsNodes,
            edges: CceBasicsEdges,
            cancellationToken).ConfigureAwait(false);

        await SeedMapAsync(
            slug: "carbon-capture",
            nameAr: "تقنيات الالتقاط الكربوني",
            nameEn: "Carbon Capture Technologies",
            descAr: "تقنيات الالتقاط من الانبعاثات الصناعية والمحيطة",
            descEn: "Capture techniques and the technologies that implement them",
            nodes: CarbonCaptureNodes,
            edges: CarbonCaptureEdges,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task SeedMapAsync(
        string slug,
        string nameAr,
        string nameEn,
        string descAr,
        string descEn,
        NodeSpec[] nodes,
        EdgeSpec[] edges,
        CancellationToken ct)
    {
        var mapId = DeterministicGuid.From($"knowledge_map:{slug}");
        var mapExists = await _ctx.KnowledgeMaps.IgnoreQueryFilters()
            .AnyAsync(m => m.Id == mapId, ct).ConfigureAwait(false);
        if (!mapExists)
        {
            var map = KnowledgeMap.Create(nameAr, nameEn, descAr, descEn, slug);
            typeof(KnowledgeMap).GetProperty(nameof(map.Id))!.SetValue(map, mapId);
            _ctx.KnowledgeMaps.Add(map);
            _logger.LogInformation("knowledge map: created {Slug}", slug);
        }

        var nodeIds = new Dictionary<string, System.Guid>();
        foreach (var n in nodes)
        {
            var id = DeterministicGuid.From($"km_node:{slug}:{n.Slug}");
            nodeIds[n.Slug] = id;
            var existing = await _ctx.KnowledgeMapNodes
                .FirstOrDefaultAsync(x => x.Id == id, ct).ConfigureAwait(false);

            if (existing is null)
            {
                var node = KnowledgeMapNode.Create(
                    mapId, n.NameAr, n.NameEn, n.Type, null, null, null, n.X, n.Y, n.Order);
                typeof(KnowledgeMapNode).GetProperty(nameof(node.Id))!.SetValue(node, id);
                _ctx.KnowledgeMapNodes.Add(node);
            }
            else
            {
                // Refresh layout so re-seeding into an older DB cleans up positions.
                existing.UpdateLayout(n.X, n.Y);
                existing.Reorder(n.Order);
            }
        }

        for (var i = 0; i < edges.Length; i++)
        {
            var e = edges[i];
            var id = DeterministicGuid.From($"km_edge:{slug}:{e.FromSlug}-{e.ToSlug}-{e.Rel}");
            var exists = await _ctx.KnowledgeMapEdges
                .AnyAsync(x => x.Id == id, ct).ConfigureAwait(false);
            if (exists) continue;
            var edge = KnowledgeMapEdge.Connect(
                mapId, nodeIds[e.FromSlug], nodeIds[e.ToSlug], e.Rel, i);
            typeof(KnowledgeMapEdge).GetProperty(nameof(edge.Id))!.SetValue(edge, id);
            _ctx.KnowledgeMapEdges.Add(edge);
        }

        await _ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation(
            "knowledge map: {Slug} → {Nodes} nodes, {Edges} edges",
            slug, nodes.Length, edges.Length);
    }
}
