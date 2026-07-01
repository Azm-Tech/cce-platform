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
        string? ParentKey);

    private const string MapKey = "co2-emissions";

    private static readonly NodeSpec[] Nodes =
    {
        // Center node
        new("co2", "ثاني أكسيد الكربون", "CO₂", "co2", null),

        // Outer nodes — major CO₂ emission sources
        new("power-generation",   "توليد الطاقة",              "Power Generation",         "power",      "co2"),
        new("transportation",     "النقل",                     "Transportation",           "transport",  "co2"),
        new("industrial",         "العمليات الصناعية",          "Industrial Processes",     "industry",   "co2"),
        new("buildings",          "المباني السكنية والتجارية",  "Residential & Commercial", "buildings",  "co2"),
        new("agriculture",        "الزراعة",                   "Agriculture",              "agriculture","co2"),
        new("oil-gas",            "النفط والغاز",               "Oil & Gas",               "oil-gas",    "co2"),
        new("cement",             "إنتاج الأسمنت",             "Cement Production",        "cement",     "co2"),
        new("chemicals",          "الصناعات الكيميائية",        "Chemical Industry",        "chemicals",  "co2"),
        new("waste",              "إدارة النفايات",            "Waste Management",         "waste",      "co2"),
        new("shipping-aviation",  "الشحن والطيران",            "Shipping & Aviation",      "shipping",   "co2"),
        new("land-use",           "استخدام الأراضي",           "Land Use & Forestry",      "land-use",   "co2"),
        new("fugitive",           "الانبعاثات المتسربة",       "Fugitive Emissions",       "fugitive",   "co2"),

        // Grandchild nodes
        new("power-coal",        "الفحم",                     "Coal",                    "coal",       "power-generation"),
        new("power-gas",         "الغاز الطبيعي",             "Natural Gas",             "gas",        "power-generation"),
        new("power-renewables",  "الطاقة المتجددة",           "Renewables",              "renewables", "power-generation"),

        new("transport-road",    "النقل البري",               "Road Transport",          "road",       "transportation"),
        new("transport-aviation","الطيران",                   "Aviation",                "aviation",   "transportation"),
        new("transport-maritime","النقل البحري",               "Maritime Transport",      "maritime",   "transportation"),

        new("industrial-steel",  "الحديد والصلب",             "Iron & Steel",            "steel",      "industrial"),
        new("industrial-refining","تكرير النفط",              "Refining",                "refining",   "industrial"),

        new("buildings-heating", "التدفئة والتبريد",           "Heating & Cooling",       "hvac",       "buildings"),
        new("buildings-lighting","الإضاءة والأجهزة",           "Lighting & Appliances",   "lighting",   "buildings"),

        new("agri-livestock",    "الماشية",                   "Livestock",               "livestock",  "agriculture"),
        new("agri-fertilizer",   "الأسمدة",                   "Fertilizers",             "fertilizer", "agriculture"),
        new("agri-rice",         "زراعة الأرز",               "Rice Cultivation",        "rice",       "agriculture"),

        new("oil-extraction",    "الاستخراج",                 "Extraction",              "extraction", "oil-gas"),
        new("oil-refining",      "التكرير",                   "Refining",                "refining",   "oil-gas"),
        new("oil-flaring",       "الحرق",                     "Flaring",                 "flaring",    "oil-gas"),

        new("cement-clinker",    "إنتاج الكلنكر",             "Clinker Production",      "clinker",    "cement"),
        new("cement-grinding",   "الطحن والتعبئة",            "Grinding & Packing",      "grinding",   "cement"),

        new("chem-ammonia",      "الأمونيا",                  "Ammonia",                 "ammonia",    "chemicals"),
        new("chem-petrochemicals","البتروكيماويات",            "Petrochemicals",          "petrochem",  "chemicals"),

        new("waste-landfill",    "المطامر",                   "Landfills",               "landfill",   "waste"),
        new("waste-incineration","الحرق",                     "Incineration",            "incineration","waste"),

        new("shipping-container","الحاويات",                  "Container Shipping",      "container",  "shipping-aviation"),
        new("shipping-freight",  "الشحن الجوي",               "Air Freight",             "freight",    "shipping-aviation"),

        new("land-deforestation","إزالة الغابات",             "Deforestation",           "deforestation","land-use"),
        new("land-peatland",     "أراضي الخث",                "Peatlands",               "peatland",   "land-use"),

        new("fugitive-coal",     "مناجم الفحم",               "Coal Mining",             "coal-mine",  "fugitive"),
        new("fugitive-pipeline", "تسرب خطوط الأنابيب",        "Pipeline Leaks",          "pipeline",   "fugitive"),
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
                interactiveMapId: mapId,
                nameAr: n.NameAr,
                nameEn: n.NameEn,
                iconKey: n.IconKey,
                category: null,
                categoryNameAr: null,
                categoryNameEn: null,
                titleAr: null,
                titleEn: null,
                descriptionAr: null,
                descriptionEn: null,
                parentId: parentId,
                topicId: topicId);
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
