using CCE.Domain.Common;
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
        await _ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static readonly System.Guid SystemAuthorId =
        DeterministicGuid.From("user:system_demo_author");

    private static readonly (string Slug, string TitleAr, string TitleEn,
        string ContentAr, string ContentEn)[] DemoNews =
    {
        ("welcome", "أهلاً بكم في منصة المعرفة", "Welcome to the Knowledge Center",
         "<p>منصة جديدة لمشاركة المعرفة...</p>", "<p>A new platform for sharing knowledge...</p>"),
        ("solar-milestone", "إنجاز جديد في الطاقة الشمسية", "New Solar Milestone",
         "<p>تم تجاوز رقم قياسي...</p>", "<p>A new world record was set...</p>"),
    };

    private async Task SeedNewsAsync(CancellationToken ct)
    {
        foreach (var n in DemoNews)
        {
            var id = DeterministicGuid.From($"news:{n.Slug}");
            var exists = await _ctx.News.IgnoreQueryFilters()
                .AnyAsync(x => x.Id == id, ct).ConfigureAwait(false);
            if (exists) continue;
            var news = News.Draft(n.TitleAr, n.TitleEn, n.ContentAr, n.ContentEn,
                n.Slug, SystemAuthorId, featuredImageUrl: null, _clock);
            typeof(News).GetProperty(nameof(news.Id))!.SetValue(news, id);
            news.Publish(_clock);
            _ctx.News.Add(news);
        }
    }

    private async Task SeedEventsAsync(CancellationToken ct)
    {
        var startsOn = _clock.UtcNow.AddDays(30);
        var endsOn = startsOn.AddHours(2);
        var id = DeterministicGuid.From("event:demo:cce-conference");
        var exists = await _ctx.Events.IgnoreQueryFilters()
            .AnyAsync(x => x.Id == id, ct).ConfigureAwait(false);
        if (exists) return;
        var ev = CCE.Domain.Content.Event.Schedule(
            "مؤتمر CCE السنوي", "CCE Annual Conference",
            "نقاش حول مستقبل الاقتصاد الكربوني", "Discussion on the future of CCE",
            startsOn, endsOn, "الرياض", "Riyadh",
            null, null, _clock);
        typeof(CCE.Domain.Content.Event).GetProperty(nameof(ev.Id))!.SetValue(ev, id);
        _ctx.Events.Add(ev);
    }
}
