using CCE.Domain.Common;
using CCE.Domain.PlatformSettings;
using CCE.Domain.PlatformSettings.ValueObjects;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CCE.Seeder.Seeders;

/// <summary>
/// Idempotent seeder that enriches the singleton PlatformSettings aggregates with
/// default child entities (glossary entries, knowledge partners, policy sections,
/// homepage country links) and richer content. Safe to run repeatedly.
/// </summary>
public sealed class PlatformSettingsSeeder : ISeeder
{
    private readonly CceDbContext _ctx;
    private readonly ISystemClock _clock;
    private readonly ILogger<PlatformSettingsSeeder> _logger;

    private static readonly Guid SystemUserId = DeterministicGuid.From("platform_settings:seeder");

    public PlatformSettingsSeeder(CceDbContext ctx, ISystemClock clock, ILogger<PlatformSettingsSeeder> logger)
    {
        _ctx = ctx;
        _clock = clock;
        _logger = logger;
    }

    public int Order => 40;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedHomepageSettingsAsync(cancellationToken).ConfigureAwait(false);
        await SeedAboutSettingsAsync(cancellationToken).ConfigureAwait(false);
        await SeedPoliciesSettingsAsync(cancellationToken).ConfigureAwait(false);
        await _ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task SeedHomepageSettingsAsync(CancellationToken ct)
    {
        var hcId = DeterministicGuid.From("platform_settings:homepage");
        var homepage = await _ctx.HomepageSettings
            .Include(h => h.Countries)
            .FirstOrDefaultAsync(h => h.Id == hcId, ct)
            .ConfigureAwait(false);

        if (homepage is null)
        {
            _logger.LogWarning("HomepageSettings singleton not found — skipping.");
            return;
        }

        // Enrich content only if still barebones
        if (string.IsNullOrEmpty(homepage.CceConceptsAr))
        {
            homepage.UpdateContent(
                videoUrl: "https://cdn.example.com/cce-hero.mp4",
                objective: LocalizedText.Create(
                    "تعزيز الاقتصاد الكربوني الدائري عبر المعرفة والابتكار",
                    "Advancing the Circular Carbon Economy through knowledge and innovation"),
                cceConceptsAr:
                    "<p>الاقتصاد الكربوني الدائري هو نهج شامل لإدارة الانبعاثات عبر تقليلها وإعادة استخدامها وتدويرها وإزالتها.</p>",
                cceConceptsEn:
                    "<p>The Circular Carbon Economy is a comprehensive approach to managing emissions through reduction, reuse, recycling, and removal.</p>",
                by: SystemUserId,
                clock: _clock);
            _logger.LogInformation("Enriched HomepageSettings content.");
        }

        // Seed homepage country links (first 5 GCC countries)
        var countryIds = new[]
        {
            DeterministicGuid.From("country:SAU"),
            DeterministicGuid.From("country:ARE"),
            DeterministicGuid.From("country:KWT"),
            DeterministicGuid.From("country:QAT"),
            DeterministicGuid.From("country:BHR"),
        };

        var existingCountryIds = homepage.Countries.Select(c => c.CountryId).ToHashSet();
        var missing = countryIds.Where(id => !existingCountryIds.Contains(id)).ToList();

        if (missing.Count > 0)
        {
            homepage.SyncCountries(countryIds, SystemUserId, _clock);
            _logger.LogInformation("Linked {Count} countries to HomepageSettings.", countryIds.Length);
        }
    }

    private async Task SeedAboutSettingsAsync(CancellationToken ct)
    {
        var acId = DeterministicGuid.From("platform_settings:about");
        var about = await _ctx.AboutSettings
            .Include(a => a.GlossaryEntries)
            .Include(a => a.KnowledgePartners)
            .FirstOrDefaultAsync(a => a.Id == acId, ct)
            .ConfigureAwait(false);

        if (about is null)
        {
            _logger.LogWarning("AboutSettings singleton not found — skipping.");
            return;
        }

        // Enrich description only if still the barebones text seeded by ReferenceDataSeeder
        if (about.Description.Ar == "وصف المنصة" && about.Description.En == "Platform description")
        {
            about.UpdateContent(
                description: LocalizedText.Create(
                    "منصة المعرفة المركزية للاقتصاد الكربوني الدائري تجمع بين الباحثين وصناع السياسات والصناعة لتبادل المعرفة وتسريع الانتقال نحو مستقبل منخفض الكربون.",
                    "The Central Knowledge Platform for the Circular Carbon Economy brings together researchers, policymakers, and industry to exchange knowledge and accelerate the transition to a low-carbon future."),
                howToUseVideoUrl: "https://cdn.example.com/how-to-use.mp4",
                by: SystemUserId,
                clock: _clock);
            _logger.LogInformation("Enriched AboutSettings content.");
        }

        // Seed glossary entries
        foreach (var g in GlossaryData)
        {
            if (await _ctx.GlossaryEntries.IgnoreQueryFilters()
                .AnyAsync(e => e.Id == g.Id, ct).ConfigureAwait(false))
            {
                continue;
            }

            var entry = about.AddGlossaryEntry(g.Term, g.Definition, SystemUserId, _clock);
            typeof(GlossaryEntry).GetProperty(nameof(entry.Id))!.SetValue(entry, g.Id);
            _logger.LogInformation("Added glossary entry: {TermEn}", g.Term.En);
        }

        // Seed knowledge partners
        foreach (var p in PartnerData)
        {
            if (await _ctx.KnowledgePartners.IgnoreQueryFilters()
                .AnyAsync(e => e.Id == p.Id, ct).ConfigureAwait(false))
            {
                continue;
            }

            var partner = about.AddKnowledgePartner(
                p.Name, p.Description, p.LogoUrl, p.WebsiteUrl, SystemUserId, _clock);
            typeof(KnowledgePartner).GetProperty(nameof(partner.Id))!.SetValue(partner, p.Id);
            _logger.LogInformation("Added knowledge partner: {NameEn}", p.Name.En);
        }
    }

    private async Task SeedPoliciesSettingsAsync(CancellationToken ct)
    {
        var pcId = DeterministicGuid.From("platform_settings:policies");
        var policies = await _ctx.PoliciesSettings
            .Include(p => p.Sections)
            .FirstOrDefaultAsync(p => p.Id == pcId, ct)
            .ConfigureAwait(false);

        if (policies is null)
        {
            _logger.LogWarning("PoliciesSettings singleton not found — skipping.");
            return;
        }

        foreach (var s in SectionData)
        {
            if (await _ctx.PolicySections.IgnoreQueryFilters()
                .AnyAsync(e => e.Id == s.Id, ct).ConfigureAwait(false))
            {
                continue;
            }

            var section = policies.AddSection(s.Type, s.Title, s.Content, SystemUserId, _clock);
            typeof(PolicySection).GetProperty(nameof(section.Id))!.SetValue(section, s.Id);
            _logger.LogInformation("Added policy section: {TitleEn}", s.Title.En);
        }
    }

    // ─── Data tables ───

    private static readonly (Guid Id, LocalizedText Term, LocalizedText Definition)[] GlossaryData =
    {
        (
            DeterministicGuid.From("glossary:cce"),
            LocalizedText.Create("الاقتصاد الكربوني الدائري", "Circular Carbon Economy"),
            LocalizedText.Create(
                "نهج شامل لإدارة الانبعاثات الكربونية يشمل الأربعة Rs: التقليل، إعادة الاستخدام، التدوير، والإزالة.",
                "A comprehensive approach to managing carbon emissions encompassing the 4 Rs: Reduce, Reuse, Recycle, and Remove.")
        ),
        (
            DeterministicGuid.From("glossary:dac"),
            LocalizedText.Create("الالتقاط المباشر من الجو", "Direct Air Capture (DAC)"),
            LocalizedText.Create(
                "تقنية لالتقاط ثاني أكسيد الكربون مباشرة من الهواء الجوي باستخدام محاليل كيميائية أو أغشية انتقالية.",
                "Technology that captures carbon dioxide directly from ambient air using chemical solutions or selective membranes.")
        ),
        (
            DeterministicGuid.From("glossary:ccus"),
            LocalizedText.Create("الاستخدام والتخزين الكربوني", "Carbon Capture, Utilization and Storage (CCUS)"),
            LocalizedText.Create(
                "عملية التقاط انبعاثات CO2 واستخدامها في منتجات أو تخزينها تحت الأرض بشكل دائم.",
                "The process of capturing CO2 emissions and either using them in products or storing them permanently underground.")
        ),
        (
            DeterministicGuid.From("glossary:lcoe"),
            LocalizedText.Create("تكلفة الطاقة المستوية", "Levelized Cost of Energy (LCOE)"),
            LocalizedText.Create(
                "تكلفة إنتاج وحدة الطاقة (عادةً MWh) على مدى عمر المشروع، تأخذ في الاعتبار الاستثمار الأولي والتشغيل والصيانة.",
                "The cost of producing a unit of energy (typically MWh) over a project lifetime, accounting for initial investment and operation & maintenance.")
        ),
    };

    private static readonly (Guid Id, LocalizedText Name, LocalizedText? Description, string? LogoUrl, string? WebsiteUrl)[] PartnerData =
    {
        (
            DeterministicGuid.From("partner:kapsarc"),
            LocalizedText.Create("كابسارك", "KAPSARC"),
            LocalizedText.Create(
                "مركز الملك عبدالله للبحوث والدراسات البترولية - مركز أبحاث عالمي مكرس لدراسة سياسات الطاقة.",
                "King Abdullah Petroleum Studies and Research Center - a global research institution dedicated to energy policy studies."),
            "https://cdn.example.com/partners/kapsarc.png",
            "https://www.kapsarc.org"
        ),
        (
            DeterministicGuid.From("partner:irena"),
            LocalizedText.Create("الوكالة الدولية للطاقة المتجددة", "IRENA"),
            LocalizedText.Create(
                "منظمة حكومية دولية تدعم انتقال الطاقة المتجددة في جميع أنحاء العالم.",
                "An intergovernmental organization that supports countries in their transition to a sustainable energy future."),
            "https://cdn.example.com/partners/irena.png",
            "https://www.irena.org"
        ),
        (
            DeterministicGuid.From("partner:gcep"),
            LocalizedText.Create("برنامج الاقتصاد الكربوني العالمي", "Global Carbon Economy Program (GCEP)"),
            LocalizedText.Create(
                "برنامج بحثي دولي يركز على تطوير تقنيات منخفضة الكربون والسياسات المرتبطة بها.",
                "An international research program focused on developing low-carbon technologies and associated policies."),
            "https://cdn.example.com/partners/gcep.png",
            "https://gcep.stanford.edu"
        ),
    };

    private static readonly (Guid Id, PolicySectionType Type, LocalizedText Title, LocalizedText Content)[] SectionData =
    {
        (
            DeterministicGuid.From("policy:terms"),
            PolicySectionType.Terms,
            LocalizedText.Create("شروط الخدمة", "Terms of Service"),
            LocalizedText.Create(
                "<h2>1. القبول بالشروط</h2><p>باستخدامك لهذه المنصة، فإنك توافق على الالتزام بهذه الشروط.</p><h2>2. الاستخدام المسموح</h2><p>يجب استخدام المنصة لأغراض قانونية فقط.</p>",
                "<h2>1. Acceptance of Terms</h2><p>By using this platform, you agree to comply with these terms.</p><h2>2. Permitted Use</h2><p>The platform must be used for lawful purposes only.</p>")
        ),
        (
            DeterministicGuid.From("policy:privacy"),
            PolicySectionType.Privacy,
            LocalizedText.Create("سياسة الخصوصية", "Privacy Policy"),
            LocalizedText.Create(
                "<h2>1. جمع البيانات</h2><p>نقوم بجمع المعلومات الضرورية لتقديم خدماتنا.</p><h2>2. حماية البيانات</h2><p>نستخدم تدابير أمنية متقدمة لحماية بياناتك.</p>",
                "<h2>1. Data Collection</h2><p>We collect information necessary to provide our services.</p><h2>2. Data Protection</h2><p>We use advanced security measures to protect your data.</p>")
        ),
        (
            DeterministicGuid.From("policy:faq"),
            PolicySectionType.FAQ,
            LocalizedText.Create("الأسئلة الشائعة", "Frequently Asked Questions"),
            LocalizedText.Create(
                "<h2>كيف أبدأ؟</h2><p>يمكنك التسجيل مجاناً والبدء في استكشاف المحتوى فوراً.</p><h2>هل المحتوى متاح بلغات متعددة؟</h2><p>نعم، المنصة تدعم اللغتين العربية والإنجليزية.</p>",
                "<h2>How do I get started?</h2><p>You can register for free and start exploring content immediately.</p><h2>Is content available in multiple languages?</h2><p>Yes, the platform supports both Arabic and English.</p>")
        ),
    };
}
