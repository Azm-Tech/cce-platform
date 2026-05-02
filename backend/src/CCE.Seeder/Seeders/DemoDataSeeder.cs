using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.Domain.Content;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CCE.Seeder.Seeders;

public sealed class DemoDataSeeder : ISeeder
{
    private readonly CceDbContext _ctx;
    private readonly ISystemClock _clock;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(CceDbContext ctx, ISystemClock clock, ILogger<DemoDataSeeder> logger)
    {
        _ctx = ctx;
        _clock = clock;
        _logger = logger;
    }

    public int Order => 100;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedNewsAsync(cancellationToken).ConfigureAwait(false);
        await SeedEventsAsync(cancellationToken).ConfigureAwait(false);
        await SeedCommunityPostsAsync(cancellationToken).ConfigureAwait(false);
        await _ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static readonly System.Guid SystemAuthorId =
        DeterministicGuid.From("user:system_demo_author");

    private static readonly (string Slug, string TitleAr, string TitleEn,
        string ContentAr, string ContentEn, bool Featured)[] DemoNews =
    {
        ("welcome",
         "أهلاً بكم في منصة المعرفة",
         "Welcome to the Knowledge Center",
         "<p>منصة جديدة لمشاركة المعرفة حول الاقتصاد الكربوني الدائري.</p>",
         "<p>A new platform for sharing knowledge about the Circular Carbon Economy.</p>",
         true),

        ("solar-milestone",
         "إنجاز جديد في الطاقة الشمسية",
         "New Solar Milestone",
         "<p>تم تجاوز رقم قياسي عالمي في كفاءة الخلايا الشمسية، مع تحقيق 33٪ في ظروف اختبار قياسية.</p>",
         "<p>A new world record was set in solar-cell efficiency, reaching 33% under standard test conditions.</p>",
         false),

        ("dac-pilot",
         "إطلاق مشروع تجريبي للالتقاط المباشر",
         "Direct Air Capture Pilot Goes Live",
         "<p>وحدة جديدة قادرة على التقاط 1000 طن من ثاني أكسيد الكربون سنوياً بدأت العمل في الرياض.</p>",
         "<p>A new unit capable of capturing 1,000 tonnes of CO₂ per year went live near Riyadh.</p>",
         true),

        ("methane-leakage",
         "تقرير: انخفاض كبير في تسرب الميثان",
         "Report: Major Drop in Methane Leakage",
         "<p>تقرير سنوي يظهر انخفاضاً بنسبة 18٪ في انبعاثات الميثان عبر القطاع.</p>",
         "<p>An annual report shows an 18% drop in methane emissions across the sector.</p>",
         false),

        ("hydrogen-corridor",
         "ممر الهيدروجين الإقليمي يبدأ المرحلة الثانية",
         "Regional Hydrogen Corridor Enters Phase II",
         "<p>توسيع ممر الهيدروجين منخفض الكربون ليشمل ثلاث دول إضافية.</p>",
         "<p>The low-carbon hydrogen corridor expands to include three additional countries.</p>",
         false),
    };

    private async Task SeedNewsAsync(CancellationToken ct)
    {
        var dayOffset = -1;
        foreach (var n in DemoNews)
        {
            var id = DeterministicGuid.From($"news:{n.Slug}");
            var exists = await _ctx.News.IgnoreQueryFilters()
                .AnyAsync(x => x.Id == id, ct).ConfigureAwait(false);
            if (exists) { dayOffset -= 7; continue; }
            var news = News.Draft(n.TitleAr, n.TitleEn, n.ContentAr, n.ContentEn,
                n.Slug, SystemAuthorId, featuredImageUrl: null, _clock);
            typeof(News).GetProperty(nameof(news.Id))!.SetValue(news, id);
            news.Publish(_clock);
            if (n.Featured)
            {
                // The Publish() method sets PublishedOn to "now"; we don't override it here
                // because there's no public ToggleFeatured today. The "isFeatured" flag is
                // surfaced as part of the entity's setter chain — left as-is for this demo.
            }
            _ctx.News.Add(news);
            dayOffset -= 7;
        }
    }

    private static readonly (string Slug, string TitleAr, string TitleEn,
        string DescAr, string DescEn, int DaysFromNow, int LengthHours,
        string LocationAr, string LocationEn, string? OnlineUrl)[] DemoEvents =
    {
        ("cce-conference",
         "مؤتمر CCE السنوي",                  "CCE Annual Conference",
         "نقاش حول مستقبل الاقتصاد الكربوني",   "Discussion on the future of CCE",
         30, 2, "الرياض",        "Riyadh", null),

        ("hydrogen-summit",
         "قمة الهيدروجين الأخضر",              "Green Hydrogen Summit",
         "أحدث التطورات في إنتاج الهيدروجين",    "Latest developments in hydrogen production",
         60, 6, "نيوم",          "Neom", null),

        ("dac-workshop",
         "ورشة الالتقاط المباشر",               "DAC Workshop",
         "ورشة عملية حول تقنيات الالتقاط",      "Hands-on workshop on capture technologies",
         15, 4, "عبر الإنترنت",   "Online", "https://meet.example.com/dac-workshop"),

        ("policy-forum",
         "منتدى السياسات المناخية",              "Climate Policy Forum",
         "حوار بين صناع السياسات والباحثين",     "Dialogue between policymakers and researchers",
         90, 8, "جدة",            "Jeddah", null),
    };

    private async Task SeedEventsAsync(CancellationToken ct)
    {
        foreach (var e in DemoEvents)
        {
            var id = DeterministicGuid.From($"event:demo:{e.Slug}");
            var exists = await _ctx.Events.IgnoreQueryFilters()
                .AnyAsync(x => x.Id == id, ct).ConfigureAwait(false);
            if (exists) continue;

            var startsOn = _clock.UtcNow.AddDays(e.DaysFromNow);
            var endsOn = startsOn.AddHours(e.LengthHours);

            var ev = CCE.Domain.Content.Event.Schedule(
                e.TitleAr, e.TitleEn,
                e.DescAr, e.DescEn,
                startsOn, endsOn,
                e.LocationAr, e.LocationEn,
                e.OnlineUrl, null, _clock);
            typeof(CCE.Domain.Content.Event).GetProperty(nameof(ev.Id))!.SetValue(ev, id);
            _ctx.Events.Add(ev);
        }
    }

    private static readonly System.Guid DemoExpertId =
        DeterministicGuid.From("user:system_demo_expert");

    private static readonly (string Slug, string TopicSlug, string Locale, bool IsAnswerable,
        string Content, (string Slug, string Content, bool IsExpert)[] Replies)[] DemoPosts =
    {
        ("solar-roi-q",
         "solar-power", "en", true,
         "What's a realistic ROI window for a 5kW residential PV system in the Gulf? " +
         "I'm trying to compare against grid-tied storage upgrades.",
         new[]
         {
             ("solar-roi-r1",
              "Most operators in the region quote 6–8 years payback assuming current tariffs. " +
              "If you couple it with a battery, expect another 2–3 years on top.",
              true),
             ("solar-roi-r2",
              "Don't forget the maintenance angle — soiling rates here are higher than the Med. " +
              "Plan for 2–3 cleanings/year.",
              false),
         }),

        ("dac-cost",
         "research", "en", false,
         "Saw the new DAC pilot near Riyadh in the news. Anyone here have insight on the " +
         "$/tonne curve at scale? The 1000 tonnes/year unit is impressive but I want to " +
         "understand the levelised cost.",
         System.Array.Empty<(string, string, bool)>()),

        ("policy-credits",
         "policy", "en", true,
         "How are GCC countries currently structuring carbon-credit registries? Is there a " +
         "regional approach forming, or is each country going its own way?",
         new[]
         {
             ("policy-credits-r1",
              "There's coordination via the GCC Secretariat but each country is publishing its " +
              "own registry. Saudi's recent regulation aligns with Article 6 of the Paris Agreement.",
              true),
         }),

        ("welcome-ar",
         "general", "ar", false,
         "أهلاً بالجميع! متحمس للانضمام إلى المنصة ومشاركة المعرفة حول الاقتصاد الكربوني الدائري.",
         System.Array.Empty<(string, string, bool)>()),
    };

    private async Task SeedCommunityPostsAsync(CancellationToken ct)
    {
        // Cache topic slug → id once, since DemoPosts references topics by slug.
        var topicMap = await _ctx.Topics
            .ToDictionaryAsync(t => t.Slug, t => t.Id, ct).ConfigureAwait(false);

        foreach (var p in DemoPosts)
        {
            if (!topicMap.TryGetValue(p.TopicSlug, out var topicId))
            {
                _logger.LogWarning(
                    "DemoDataSeeder: topic '{Slug}' missing — skipping post '{PostSlug}'.",
                    p.TopicSlug, p.Slug);
                continue;
            }

            var postId = DeterministicGuid.From($"post:demo:{p.Slug}");
            var postExists = await _ctx.Posts.IgnoreQueryFilters()
                .AnyAsync(x => x.Id == postId, ct).ConfigureAwait(false);

            if (!postExists)
            {
                var post = Post.Create(topicId, SystemAuthorId, p.Content,
                    p.Locale, p.IsAnswerable, _clock);
                typeof(Post).GetProperty(nameof(post.Id))!.SetValue(post, postId);
                _ctx.Posts.Add(post);
            }

            foreach (var r in p.Replies)
            {
                var replyId = DeterministicGuid.From($"reply:demo:{p.Slug}:{r.Slug}");
                var replyExists = await _ctx.PostReplies.IgnoreQueryFilters()
                    .AnyAsync(x => x.Id == replyId, ct).ConfigureAwait(false);
                if (replyExists) continue;

                var authorId = r.IsExpert ? DemoExpertId : SystemAuthorId;
                var reply = PostReply.Create(postId, authorId, r.Content,
                    p.Locale, parentReplyId: null, isByExpert: r.IsExpert, _clock);
                typeof(PostReply).GetProperty(nameof(reply.Id))!.SetValue(reply, replyId);
                _ctx.PostReplies.Add(reply);
            }
        }
    }
}
