namespace CCE.Application.Content;

using CCE.Domain.Content;

public static class ResourceTypeAr
{
    private static readonly System.Collections.Generic.Dictionary<ResourceType, string> Map = new()
    {
        [ResourceType.Paper] = "ورقة",
        [ResourceType.Article] = "مقال",
        [ResourceType.Study] = "دراسة",
        [ResourceType.Presentation] = "عرض تقديمي",
        [ResourceType.ScientificPaper] = "ورقة علمية",
        [ResourceType.Report] = "تقرير",
        [ResourceType.Book] = "كتاب",
        [ResourceType.Research] = "بحث",
        [ResourceType.CceGuide] = "دليل CCE",
        [ResourceType.Media] = "وسائط",
    };

    public static string Get(ResourceType type) => Map.GetValueOrDefault(type, type.ToString());
}
