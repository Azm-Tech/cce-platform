using CCE.Domain.Content;
using CCE.Domain.InteractiveMaps;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CCE.Seeder.Seeders;

public sealed class InteractiveMapSeeder : ISeeder
{
    private readonly CceDbContext _ctx;
    private readonly ILogger<InteractiveMapSeeder> _logger;

    public InteractiveMapSeeder(CceDbContext ctx, ILogger<InteractiveMapSeeder> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }

    public int Order => 35;

    private sealed record NodeSpec(
        string Key,
        string NameAr,
        string NameEn,
        string IconKey,
        int Level,
        string? ParentKey);

    private const string MapKey = "co2-emissions";

    private static readonly NodeSpec[] Nodes =
    {
        // Center node (level 0)
        new("co2", "ثاني أكسيد الكربون", "CO₂", "co2", 0, null),

        // Outer nodes (level 1) — major CO₂ emission sources
        new("power-generation",   "توليد الطاقة",              "Power Generation",         "power",      1, "co2"),
        new("transportation",     "النقل",                     "Transportation",           "transport",  1, "co2"),
        new("industrial",         "العمليات الصناعية",          "Industrial Processes",     "industry",   1, "co2"),
        new("buildings",          "المباني السكنية والتجارية",  "Residential & Commercial", "buildings",  1, "co2"),
        new("agriculture",        "الزراعة",                   "Agriculture",              "agriculture",1, "co2"),
        new("oil-gas",            "النفط والغاز",               "Oil & Gas",               "oil-gas",    1, "co2"),
        new("cement",             "إنتاج الأسمنت",             "Cement Production",        "cement",     1, "co2"),
        new("chemicals",          "الصناعات الكيميائية",        "Chemical Industry",        "chemicals",  1, "co2"),
        new("waste",              "إدارة النفايات",            "Waste Management",         "waste",      1, "co2"),
        new("shipping-aviation",  "الشحن والطيران",            "Shipping & Aviation",      "shipping",   1, "co2"),
        new("land-use",           "استخدام الأراضي",           "Land Use & Forestry",      "land-use",   1, "co2"),
        new("fugitive",           "الانبعاثات المتسربة",       "Fugitive Emissions",       "fugitive",   1, "co2"),

        // Grandchild nodes (level 2)
        new("power-coal",        "الفحم",                     "Coal",                    "coal",       2, "power-generation"),
        new("power-gas",         "الغاز الطبيعي",             "Natural Gas",             "gas",        2, "power-generation"),
        new("power-renewables",  "الطاقة المتجددة",           "Renewables",              "renewables", 2, "power-generation"),

        new("transport-road",    "النقل البري",               "Road Transport",          "road",       2, "transportation"),
        new("transport-aviation","الطيران",                   "Aviation",                "aviation",   2, "transportation"),
        new("transport-maritime","النقل البحري",               "Maritime Transport",      "maritime",   2, "transportation"),

        new("industrial-steel",  "الحديد والصلب",             "Iron & Steel",            "steel",      2, "industrial"),
        new("industrial-refining","تكرير النفط",              "Refining",                "refining",   2, "industrial"),

        new("buildings-heating", "التدفئة والتبريد",           "Heating & Cooling",       "hvac",       2, "buildings"),
        new("buildings-lighting","الإضاءة والأجهزة",           "Lighting & Appliances",   "lighting",   2, "buildings"),

        new("agri-livestock",    "الماشية",                   "Livestock",               "livestock",  2, "agriculture"),
        new("agri-fertilizer",   "الأسمدة",                   "Fertilizers",             "fertilizer", 2, "agriculture"),
        new("agri-rice",         "زراعة الأرز",               "Rice Cultivation",        "rice",       2, "agriculture"),

        new("oil-extraction",    "الاستخراج",                 "Extraction",              "extraction", 2, "oil-gas"),
        new("oil-refining",      "التكرير",                   "Refining",                "refining",   2, "oil-gas"),
        new("oil-flaring",       "الحرق",                     "Flaring",                 "flaring",    2, "oil-gas"),

        new("cement-clinker",    "إنتاج الكلنكر",             "Clinker Production",      "clinker",    2, "cement"),
        new("cement-grinding",   "الطحن والتعبئة",            "Grinding & Packing",      "grinding",   2, "cement"),

        new("chem-ammonia",      "الأمونيا",                  "Ammonia",                 "ammonia",    2, "chemicals"),
        new("chem-petrochemicals","البتروكيماويات",            "Petrochemicals",          "petrochem",  2, "chemicals"),

        new("waste-landfill",    "المطامر",                   "Landfills",               "landfill",   2, "waste"),
        new("waste-incineration","الحرق",                     "Incineration",            "incineration",2, "waste"),

        new("shipping-container","الحاويات",                  "Container Shipping",      "container",  2, "shipping-aviation"),
        new("shipping-freight",  "الشحن الجوي",               "Air Freight",             "freight",    2, "shipping-aviation"),

        new("land-deforestation","إزالة الغابات",             "Deforestation",           "deforestation",2, "land-use"),
        new("land-peatland",     "أراضي الخث",                "Peatlands",               "peatland",   2, "land-use"),

        new("fugitive-coal",     "مناجم الفحم",               "Coal Mining",             "coal-mine",  2, "fugitive"),
        new("fugitive-pipeline", "تسرب خطوط الأنابيب",        "Pipeline Leaks",          "pipeline",   2, "fugitive"),
    };

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var mapId = DeterministicGuid.From($"interactive_map:{MapKey}");
        var mapExists = await _ctx.InteractiveMaps
            .IgnoreQueryFilters()
            .AnyAsync(m => m.Id == mapId, cancellationToken)
            .ConfigureAwait(false);

        if (!mapExists)
        {
            var map = InteractiveMap.Create(
                "انبعاثات ثاني أكسيد الكربون",
                "CO₂ Emissions",
                "خريطة تفاعلية لمصادر انبعاثات ثاني أكسيد الكربون",
                "An interactive map of CO₂ emission sources");
            typeof(InteractiveMap).GetProperty(nameof(map.Id))!.SetValue(map, mapId);

            _ctx.InteractiveMaps.Add(map);
            _logger.LogInformation("interactive map: created");
        }

        var tag = await _ctx.Tags
            .FirstOrDefaultAsync(t => t.NameEn == "Emissions", cancellationToken)
            .ConfigureAwait(false);

        var topicId = new System.Guid("36BD1319-8965-AAF2-F5DC-76E849C2C53C");

        var nodeIds = new Dictionary<string, Guid>();
        foreach (var n in Nodes)
        {
            var id = DeterministicGuid.From($"im_node:{MapKey}:{n.Key}");
            nodeIds[n.Key] = id;

            var exists = await _ctx.InteractiveMapNodes
                .IgnoreQueryFilters()
                .AnyAsync(x => x.Id == id, cancellationToken)
                .ConfigureAwait(false);
            if (exists) continue;

            var parentId = n.ParentKey is not null ? nodeIds[n.ParentKey] : (Guid?)null;
            var node = InteractiveMapNode.Create(
                mapId, n.NameAr, n.NameEn, n.IconKey,
                category: null, categoryNameAr: null, categoryNameEn: null,
                n.Level, parentId, topicId);
            typeof(InteractiveMapNode).GetProperty(nameof(node.Id))!.SetValue(node, id);

            if (tag is not null)
                node.SetTags([tag]);

            _ctx.InteractiveMapNodes.Add(node);
            _logger.LogInformation("  node: created {Key}", n.Key);
        }

        await _ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("interactive map: {Key} → {Count} nodes", MapKey, Nodes.Length);
    }
}
