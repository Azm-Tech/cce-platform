using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.Domain.Content;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CCE.Seeder.Seeders;

/// <summary>
/// Reference-data seeder. Populates lookup tables (countries, categories, topics, technologies,
/// templates, knowledge maps, pages, homepage sections) with values that should exist in every
/// environment. Idempotent.
/// </summary>
public sealed class ReferenceDataSeeder : ISeeder
{
    private readonly CceDbContext _ctx;
    private readonly ISystemClock _clock;
    private readonly ILogger<ReferenceDataSeeder> _logger;

    public ReferenceDataSeeder(CceDbContext ctx, ISystemClock clock, ILogger<ReferenceDataSeeder> logger)
    {
        _ctx = ctx;
        _clock = clock;
        _logger = logger;
    }

    public int Order => 20;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedCountriesAsync(cancellationToken).ConfigureAwait(false);
        await SeedResourceCategoriesAsync(cancellationToken).ConfigureAwait(false);
        await SeedTopicsAsync(cancellationToken).ConfigureAwait(false);
        await SeedCityTechnologiesAsync(cancellationToken).ConfigureAwait(false);
        await SeedNotificationTemplatesAsync(cancellationToken).ConfigureAwait(false);
        await SeedStaticPagesAsync(cancellationToken).ConfigureAwait(false);
        await SeedHomepageSectionsAsync(cancellationToken).ConfigureAwait(false);
        await _ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static readonly (string Iso3, string Iso2, string NameAr, string NameEn,
        string RegionAr, string RegionEn)[] InitialCountries =
    {
        ("SAU", "SA", "السعودية", "Saudi Arabia", "آسيا", "Asia"),
        ("ARE", "AE", "الإمارات", "United Arab Emirates", "آسيا", "Asia"),
        ("KWT", "KW", "الكويت", "Kuwait", "آسيا", "Asia"),
        ("QAT", "QA", "قطر", "Qatar", "آسيا", "Asia"),
        ("BHR", "BH", "البحرين", "Bahrain", "آسيا", "Asia"),
        ("OMN", "OM", "عُمان", "Oman", "آسيا", "Asia"),
        ("EGY", "EG", "مصر", "Egypt", "أفريقيا", "Africa"),
        ("JOR", "JO", "الأردن", "Jordan", "آسيا", "Asia"),
    };

    private async Task SeedCountriesAsync(CancellationToken ct)
    {
        foreach (var c in InitialCountries)
        {
            var id = DeterministicGuid.From($"country:{c.Iso3}");
            var exists = await _ctx.Countries.IgnoreQueryFilters()
                .AnyAsync(x => x.Id == id, ct).ConfigureAwait(false);
            if (exists) continue;

            var country = CCE.Domain.Country.Country.Register(
                c.Iso3, c.Iso2, c.NameAr, c.NameEn, c.RegionAr, c.RegionEn,
                $"https://flags.example.com/{c.Iso2.ToLowerInvariant()}.svg");
            typeof(CCE.Domain.Country.Country).GetProperty(nameof(country.Id))!
                .SetValue(country, id);
            _ctx.Countries.Add(country);
        }
    }

    private static readonly (string Slug, string NameAr, string NameEn)[] InitialCategories =
    {
        ("solar", "الطاقة الشمسية", "Solar Energy"),
        ("wind", "طاقة الرياح", "Wind Energy"),
        ("storage", "التخزين", "Energy Storage"),
        ("hydrogen", "الهيدروجين", "Hydrogen"),
        ("efficiency", "كفاءة الطاقة", "Energy Efficiency"),
        ("policy", "السياسات", "Policy & Regulation"),
    };

    private async Task SeedResourceCategoriesAsync(CancellationToken ct)
    {
        for (var i = 0; i < InitialCategories.Length; i++)
        {
            var c = InitialCategories[i];
            var id = DeterministicGuid.From($"resource_category:{c.Slug}");
            var exists = await _ctx.ResourceCategories
                .AnyAsync(x => x.Id == id, ct).ConfigureAwait(false);
            if (exists) continue;
            var cat = ResourceCategory.Create(c.NameAr, c.NameEn, c.Slug, parentId: null, orderIndex: i);
            typeof(ResourceCategory).GetProperty(nameof(cat.Id))!.SetValue(cat, id);
            _ctx.ResourceCategories.Add(cat);
        }
    }

    private static readonly (string Slug, string NameAr, string NameEn,
        string DescriptionAr, string DescriptionEn)[] InitialTopics =
    {
        ("general", "عام", "General", "نقاشات عامة", "General discussions"),
        ("solar-power", "الطاقة الشمسية", "Solar Power", "كل ما يخص الطاقة الشمسية", "All about solar power"),
        ("policy", "السياسات", "Policy", "السياسات والتشريعات", "Policy and regulation"),
        ("research", "الأبحاث", "Research", "الأبحاث الحديثة", "Latest research"),
    };

    private async Task SeedTopicsAsync(CancellationToken ct)
    {
        for (var i = 0; i < InitialTopics.Length; i++)
        {
            var t = InitialTopics[i];
            var id = DeterministicGuid.From($"topic:{t.Slug}");
            var exists = await _ctx.Topics
                .AnyAsync(x => x.Id == id, ct).ConfigureAwait(false);
            if (exists) continue;
            var topic = Topic.Create(t.NameAr, t.NameEn, t.DescriptionAr, t.DescriptionEn,
                t.Slug, parentId: null, iconUrl: null, orderIndex: i);
            typeof(Topic).GetProperty(nameof(topic.Id))!.SetValue(topic, id);
            _ctx.Topics.Add(topic);
        }
    }

    private static readonly (string Slug, string NameAr, string NameEn,
        string DescriptionAr, string DescriptionEn,
        string CategoryAr, string CategoryEn,
        decimal CarbonImpact, decimal Cost)[] InitialCityTechs =
    {
        ("solar-rooftop", "ألواح شمسية على الأسطح", "Rooftop Solar Panels",
         "نظام كهروضوئي سكني بقدرة 5 ك.و", "5kW residential PV system",
         "الطاقة المتجددة", "Renewable Energy", -2500m, 12000m),
        ("ev-charging", "شواحن السيارات الكهربائية", "EV Charging Stations",
         "محطات شحن سريعة للسيارات الكهربائية", "Fast-charging stations",
         "النقل", "Transportation", -1800m, 8000m),
        ("led-lighting", "إنارة LED", "LED Lighting",
         "ترقية شاملة لإنارة LED", "Building-wide LED retrofit",
         "كفاءة الطاقة", "Energy Efficiency", -500m, 3000m),
        ("heat-pump", "مضخة حرارية", "Heat Pump",
         "مضخة حرارية للتدفئة والتبريد", "HVAC heat-pump system",
         "كفاءة الطاقة", "Energy Efficiency", -1200m, 7500m),
    };

    private async Task SeedCityTechnologiesAsync(CancellationToken ct)
    {
        foreach (var t in InitialCityTechs)
        {
            var id = DeterministicGuid.From($"city_tech:{t.Slug}");
            var exists = await _ctx.CityTechnologies
                .AnyAsync(x => x.Id == id, ct).ConfigureAwait(false);
            if (exists) continue;
            var tech = CCE.Domain.InteractiveCity.CityTechnology.Create(
                t.NameAr, t.NameEn, t.DescriptionAr, t.DescriptionEn,
                t.CategoryAr, t.CategoryEn, t.CarbonImpact, t.Cost);
            typeof(CCE.Domain.InteractiveCity.CityTechnology)
                .GetProperty(nameof(tech.Id))!.SetValue(tech, id);
            _ctx.CityTechnologies.Add(tech);
        }
    }

    private static readonly (string Code, string SubjectAr, string SubjectEn,
        string BodyAr, string BodyEn,
        CCE.Domain.Notifications.NotificationChannel Channel)[] InitialTemplates =
    {
        ("ACCOUNT_CREATED", "تم إنشاء حسابك", "Your account is created",
         "مرحباً {{Name}}، تم إنشاء حسابك بنجاح.", "Hi {{Name}}, your account is now active.",
         CCE.Domain.Notifications.NotificationChannel.Email),
        ("EXPERT_REQUEST_APPROVED", "تمت الموافقة على طلبك", "Your expert request was approved",
         "مرحباً {{Name}}، تمت الموافقة على طلب الخبير الخاص بك.",
         "Hi {{Name}}, your expert-registration request has been approved.",
         CCE.Domain.Notifications.NotificationChannel.Email),
        ("EXPERT_REQUEST_REJECTED", "تم رفض طلبك", "Your expert request was rejected",
         "نأسف، تم رفض طلب الخبير: {{Reason}}", "Sorry, your expert request was rejected: {{Reason}}",
         CCE.Domain.Notifications.NotificationChannel.Email),
        ("RESOURCE_REQUEST_APPROVED", "تمت الموافقة على المورد", "Country resource approved",
         "تمت الموافقة على مساهمة الدولة الخاصة بك.", "Your country resource submission was approved.",
         CCE.Domain.Notifications.NotificationChannel.InApp),
    };

    private async Task SeedNotificationTemplatesAsync(CancellationToken ct)
    {
        foreach (var t in InitialTemplates)
        {
            var id = DeterministicGuid.From($"template:{t.Code}");
            var exists = await _ctx.NotificationTemplates
                .AnyAsync(x => x.Id == id, ct).ConfigureAwait(false);
            if (exists) continue;
            var template = CCE.Domain.Notifications.NotificationTemplate.Define(
                t.Code, t.SubjectAr, t.SubjectEn, t.BodyAr, t.BodyEn, t.Channel, "{}");
            typeof(CCE.Domain.Notifications.NotificationTemplate)
                .GetProperty(nameof(template.Id))!.SetValue(template, id);
            _ctx.NotificationTemplates.Add(template);
        }
    }

    private static readonly (string Slug, CCE.Domain.Content.PageType Type,
        string TitleAr, string TitleEn, string ContentAr, string ContentEn)[] InitialPages =
    {
        ("about", CCE.Domain.Content.PageType.AboutPlatform,
         "عن المنصة", "About the Platform",
         "<p>منصة المعرفة للاقتصاد الكربوني الدائري...</p>",
         "<p>The Circular Carbon Economy Knowledge Center...</p>"),
        ("terms", CCE.Domain.Content.PageType.TermsOfService,
         "شروط الاستخدام", "Terms of Service",
         "<p>تطبق شروط الاستخدام التالية...</p>",
         "<p>The following terms of service apply...</p>"),
        ("privacy", CCE.Domain.Content.PageType.PrivacyPolicy,
         "سياسة الخصوصية", "Privacy Policy",
         "<p>نلتزم بحماية بياناتك الشخصية...</p>",
         "<p>We are committed to protecting your data...</p>"),
    };

    private async Task SeedStaticPagesAsync(CancellationToken ct)
    {
        foreach (var p in InitialPages)
        {
            var id = DeterministicGuid.From($"page:{p.Type}:{p.Slug}");
            var exists = await _ctx.Pages.IgnoreQueryFilters()
                .AnyAsync(x => x.Id == id, ct).ConfigureAwait(false);
            if (exists) continue;
            var page = CCE.Domain.Content.Page.Create(
                p.Slug, p.Type, p.TitleAr, p.TitleEn, p.ContentAr, p.ContentEn);
            typeof(CCE.Domain.Content.Page)
                .GetProperty(nameof(page.Id))!.SetValue(page, id);
            _ctx.Pages.Add(page);
        }
    }

    private static readonly (CCE.Domain.Content.HomepageSectionType Type, int Order,
        string ContentAr, string ContentEn)[] InitialSections =
    {
        (CCE.Domain.Content.HomepageSectionType.Hero, 0,
         "{\"titleAr\": \"معاً نحو اقتصاد كربوني دائري\"}",
         "{\"titleEn\": \"Together towards a circular carbon economy\"}"),
        (CCE.Domain.Content.HomepageSectionType.FeaturedNews, 1, "{}", "{}"),
        (CCE.Domain.Content.HomepageSectionType.FeaturedResources, 2, "{}", "{}"),
        (CCE.Domain.Content.HomepageSectionType.UpcomingEvents, 3, "{}", "{}"),
        (CCE.Domain.Content.HomepageSectionType.NewsletterSignup, 4, "{}", "{}"),
    };

    private async Task SeedHomepageSectionsAsync(CancellationToken ct)
    {
        foreach (var s in InitialSections)
        {
            var id = DeterministicGuid.From($"homepage_section:{s.Type}:{s.Order}");
            var exists = await _ctx.HomepageSections.IgnoreQueryFilters()
                .AnyAsync(x => x.Id == id, ct).ConfigureAwait(false);
            if (exists) continue;
            var section = CCE.Domain.Content.HomepageSection.Create(
                s.Type, s.Order, s.ContentAr, s.ContentEn);
            typeof(CCE.Domain.Content.HomepageSection)
                .GetProperty(nameof(section.Id))!.SetValue(section, id);
            _ctx.HomepageSections.Add(section);
        }
    }
}
