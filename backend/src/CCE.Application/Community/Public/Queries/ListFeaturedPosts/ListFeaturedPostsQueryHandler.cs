using CCE.Application.Common;
using CCE.Application.Common.Pagination;
using CCE.Application.Community.Public.Dtos;
using CCE.Application.Messages;

using MediatR;

namespace CCE.Application.Community.Public.Queries.ListFeaturedPosts;

/// <summary>
/// TEMP: returns a fixed mock list of popular posts so the feed can be wired up
/// front-to-back before the real popularity query is enabled. Replace
/// <see cref="MockPosts"/> with the EF-backed query when ready.
/// </summary>
public sealed class ListFeaturedPostsQueryHandler
    : IRequestHandler<ListFeaturedPostsQuery, Response<PagedResult<FeaturedPostDto>>>
{
    private readonly MessageFactory _messages;

    public ListFeaturedPostsQueryHandler(MessageFactory messages)
    {
        _messages = messages;
    }

    public Task<Response<PagedResult<FeaturedPostDto>>> Handle(
        ListFeaturedPostsQuery request,
        CancellationToken cancellationToken)
    {
        var all = request.TopicId.HasValue
            ? MockPosts.Where(p => p.TopicId == request.TopicId.Value).ToList()
            : MockPosts;

        var page = System.Math.Max(1, request.Page);
        var pageSize = System.Math.Clamp(request.PageSize, 1, 100);

        var items = all
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = new PagedResult<FeaturedPostDto>(items, page, pageSize, all.Count);
        return Task.FromResult(_messages.Ok(result, MessageKeys.General.SUCCESS_OPERATION));
    }

    // ─── Mock data (deterministic ids + fixed timestamps) ─────────────────────
    private static readonly System.Guid CarbonTopicId = System.Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly System.Guid PolicyTopicId = System.Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly System.Collections.Generic.List<FeaturedPostDto> MockPosts = new()
    {
        new(System.Guid.Parse("a0000000-0000-0000-0000-000000000001"), CarbonTopicId,
            "الاقتصاد الدائري للكربون", "Circular Carbon Economy",
            "نظرة عامة على ركائز الاقتصاد الدائري للكربون: التقليل، إعادة الاستخدام، التدوير، والإزالة.",
            "ar", System.Guid.Parse("c0000000-0000-0000-0000-000000000001"), "Layla Hassan",
            new System.DateTimeOffset(2026, 5, 2, 9, 0, 0, System.TimeSpan.Zero), 42, 4.8, 17),

        new(System.Guid.Parse("a0000000-0000-0000-0000-000000000002"), CarbonTopicId,
            "احتجاز الكربون", "Carbon Capture",
            "How carbon capture and storage technologies are scaling across the region's heavy industries.",
            "en", System.Guid.Parse("c0000000-0000-0000-0000-000000000002"), "Omar Khalid",
            new System.DateTimeOffset(2026, 4, 28, 13, 30, 0, System.TimeSpan.Zero), 38, 4.6, 23),

        new(System.Guid.Parse("a0000000-0000-0000-0000-000000000003"), CarbonTopicId,
            "البصمة الكربونية", "Carbon Footprint",
            "خطوات عملية لقياس البصمة الكربونية للمنشآت الصناعية وخفضها تدريجياً.",
            "ar", System.Guid.Parse("c0000000-0000-0000-0000-000000000003"), "Sara Mansour",
            new System.DateTimeOffset(2026, 4, 21, 8, 15, 0, System.TimeSpan.Zero), 31, 4.5, 9),

        new(System.Guid.Parse("a0000000-0000-0000-0000-000000000004"), PolicyTopicId,
            "سياسات الطاقة المتجددة", "Renewable Energy Policy",
            "A discussion on incentive structures driving renewable adoption and grid integration.",
            "en", System.Guid.Parse("c0000000-0000-0000-0000-000000000004"), "Yousef Al-Otaibi",
            new System.DateTimeOffset(2026, 4, 18, 11, 45, 0, System.TimeSpan.Zero), 27, 4.3, 14),

        new(System.Guid.Parse("a0000000-0000-0000-0000-000000000005"), CarbonTopicId,
            "الهيدروجين الأخضر", "Green Hydrogen",
            "دور الهيدروجين الأخضر في إزالة الكربون من قطاعات النقل والصناعة الثقيلة.",
            "ar", System.Guid.Parse("c0000000-0000-0000-0000-000000000005"), "Noura Saleh",
            new System.DateTimeOffset(2026, 4, 12, 16, 0, 0, System.TimeSpan.Zero), 24, 4.7, 8),

        new(System.Guid.Parse("a0000000-0000-0000-0000-000000000006"), PolicyTopicId,
            "تسعير الكربون", "Carbon Pricing",
            "Comparing carbon tax versus cap-and-trade approaches and their regional applicability.",
            "en", System.Guid.Parse("c0000000-0000-0000-0000-000000000006"), "Khalid Nasser",
            new System.DateTimeOffset(2026, 4, 5, 10, 20, 0, System.TimeSpan.Zero), 19, 4.1, 11),

        new(System.Guid.Parse("a0000000-0000-0000-0000-000000000007"), CarbonTopicId,
            "كفاءة الطاقة", "Energy Efficiency",
            "أفضل الممارسات لتحسين كفاءة الطاقة في المباني والمصانع.",
            "ar", System.Guid.Parse("c0000000-0000-0000-0000-000000000007"), "Maha Abdullah",
            new System.DateTimeOffset(2026, 3, 30, 14, 10, 0, System.TimeSpan.Zero), 16, 4.0, 6),

        new(System.Guid.Parse("a0000000-0000-0000-0000-000000000008"), CarbonTopicId,
            "إعادة التحريج", "Reforestation",
            "Nature-based carbon removal: how large-scale reforestation contributes to net-zero targets.",
            "en", System.Guid.Parse("c0000000-0000-0000-0000-000000000008"), "Faisal Tariq",
            new System.DateTimeOffset(2026, 3, 22, 9, 50, 0, System.TimeSpan.Zero), 13, 3.9, 4),

        new(System.Guid.Parse("a0000000-0000-0000-0000-000000000009"), PolicyTopicId,
            "الحياد الكربوني 2060", "Net Zero 2060",
            "مسار تحقيق الحياد الكربوني بحلول عام 2060 والمحطات الرئيسية على الطريق.",
            "ar", System.Guid.Parse("c0000000-0000-0000-0000-000000000009"), "Reem Al-Harbi",
            new System.DateTimeOffset(2026, 3, 15, 12, 0, 0, System.TimeSpan.Zero), 11, 4.2, 7),

        new(System.Guid.Parse("a0000000-0000-0000-0000-00000000000a"), CarbonTopicId,
            "الوقود المستدام", "Sustainable Fuels",
            "An overview of synthetic and bio-based fuels as transitional decarbonization levers.",
            "en", System.Guid.Parse("c0000000-0000-0000-0000-00000000000a"), "Tariq Salem",
            new System.DateTimeOffset(2026, 3, 8, 15, 25, 0, System.TimeSpan.Zero), 8, 3.7, 3),

        new(System.Guid.Parse("a0000000-0000-0000-0000-00000000000b"), CarbonTopicId,
            "التقاط الميثان", "Methane Capture",
            "تقنيات الحد من انبعاثات الميثان في قطاع النفط والغاز.",
            "ar", System.Guid.Parse("c0000000-0000-0000-0000-00000000000b"), "Hana Yousef",
            new System.DateTimeOffset(2026, 3, 1, 7, 40, 0, System.TimeSpan.Zero), 6, 3.5, 2),

        new(System.Guid.Parse("a0000000-0000-0000-0000-00000000000c"), PolicyTopicId,
            "التمويل الأخضر", "Green Finance",
            "How green bonds and sustainability-linked loans fund the energy transition.",
            "en", System.Guid.Parse("c0000000-0000-0000-0000-00000000000c"), "Ahmed Zaki",
            new System.DateTimeOffset(2026, 2, 22, 10, 5, 0, System.TimeSpan.Zero), 4, 3.2, 1),
    };
}
