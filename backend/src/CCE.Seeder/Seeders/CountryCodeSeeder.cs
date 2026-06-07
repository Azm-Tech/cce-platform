using CCE.Domain.Common;
using CCE.Domain.Lookups;
using CCE.Domain.PlatformSettings.ValueObjects;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CCE.Seeder.Seeders;

/// <summary>
/// Idempotent seeder for the <see cref="CountryCode"/> lookup table.
/// Seeds all real-world countries with dial codes. Israel is excluded;
/// Palestine is included with the +972 dial code.
/// </summary>
public sealed class CountryCodeSeeder : ISeeder
{
    private readonly CceDbContext _ctx;
    private readonly ISystemClock _clock;
    private readonly ILogger<CountryCodeSeeder> _logger;

    public CountryCodeSeeder(CceDbContext ctx, ISystemClock clock, ILogger<CountryCodeSeeder> logger)
    {
        _ctx = ctx;
        _clock = clock;
        _logger = logger;
    }

    public int Order => 25;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var systemUser = DeterministicGuid.From("country_code:seeder");
        var seeded = 0;
        var skipped = 0;

        foreach (var c in CountryCodes)
        {
            var id = DeterministicGuid.From($"country_code:{c.NameEn}");
            var exists = await _ctx.CountryCodes.IgnoreQueryFilters()
                .AnyAsync(x => x.Id == id, cancellationToken)
                .ConfigureAwait(false);
            if (exists)
            {
                skipped++;
                continue;
            }

            var name = LocalizedText.Create(c.NameAr, c.NameEn);
            var entity = CountryCode.Create(name, c.DialCode, c.FlagUrl, systemUser, _clock);
            typeof(CountryCode).GetProperty(nameof(entity.Id))!.SetValue(entity, id);
            _ctx.CountryCodes.Add(entity);
            seeded++;
        }

        await _ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "CountryCode seeder finished — seeded {Seeded}, skipped {Skipped}.", seeded, skipped);
    }

    // Data: (NameAr, NameEn, DialCode, FlagUrl)
    // Israel is intentionally omitted. Palestine uses +972.
    private static readonly (string NameAr, string NameEn, string DialCode, string? FlagUrl)[] CountryCodes =
    {
        ("أفغانستان", "Afghanistan", "+93", null),
        ("ألبانيا", "Albania", "+355", null),
        ("الجزائر", "Algeria", "+213", null),
        ("أندورا", "Andorra", "+376", null),
        ("أنغولا", "Angola", "+244", null),
        ("أنتيغوا وبربودا", "Antigua and Barbuda", "+1-268", null),
        ("الأرجنتين", "Argentina", "+54", null),
        ("أرمينيا", "Armenia", "+374", null),
        ("أستراليا", "Australia", "+61", null),
        ("النمسا", "Austria", "+43", null),
        ("أذربيجان", "Azerbaijan", "+994", null),
        ("الباهاما", "Bahamas", "+1-242", null),
        ("البحرين", "Bahrain", "+973", null),
        ("بنغلاديش", "Bangladesh", "+880", null),
        ("باربادوس", "Barbados", "+1-246", null),
        ("بيلاروس", "Belarus", "+375", null),
        ("بلجيكا", "Belgium", "+32", null),
        ("بليز", "Belize", "+501", null),
        ("بنين", "Benin", "+229", null),
        ("بوتان", "Bhutan", "+975", null),
        ("بوليفيا", "Bolivia", "+591", null),
        ("البوسنة والهرسك", "Bosnia and Herzegovina", "+387", null),
        ("بوتسوانا", "Botswana", "+267", null),
        ("البرازيل", "Brazil", "+55", null),
        ("بروناي", "Brunei", "+673", null),
        ("بلغاريا", "Bulgaria", "+359", null),
        ("بوركينا فاسو", "Burkina Faso", "+226", null),
        ("بوروندي", "Burundi", "+257", null),
        ("كابو فيردي", "Cabo Verde", "+238", null),
        ("كمبوديا", "Cambodia", "+855", null),
        ("الكاميرون", "Cameroon", "+237", null),
        ("كندا", "Canada", "+1", null),
        ("جمهورية أفريقيا الوسطى", "Central African Republic", "+236", null),
        ("تشاد", "Chad", "+235", null),
        ("تشيلي", "Chile", "+56", null),
        ("الصين", "China", "+86", null),
        ("كولومبيا", "Colombia", "+57", null),
        ("جزر القمر", "Comoros", "+269", null),
        ("الكونغو", "Congo", "+242", null),
        ("الكونغو (الديمقراطية)", "Congo (DRC)", "+243", null),
        ("كوستاريكا", "Costa Rica", "+506", null),
        ("كرواتيا", "Croatia", "+385", null),
        ("كوبا", "Cuba", "+53", null),
        ("قبرص", "Cyprus", "+357", null),
        ("التشيك", "Czech Republic", "+420", null),
        ("الدانمرك", "Denmark", "+45", null),
        ("جيبوتي", "Djibouti", "+253", null),
        ("دومينيكا", "Dominica", "+1-767", null),
        ("جمهورية الدومينيكان", "Dominican Republic", "+1-809", null),
        ("تيمور الشرقية", "East Timor", "+670", null),
        ("الإكوادور", "Ecuador", "+593", null),
        ("مصر", "Egypt", "+20", null),
        ("السلفادور", "El Salvador", "+503", null),
        ("غينيا الاستوائية", "Equatorial Guinea", "+240", null),
        ("إريتريا", "Eritrea", "+291", null),
        ("إستونيا", "Estonia", "+372", null),
        ("إسواتيني", "Eswatini", "+268", null),
        ("إثيوبيا", "Ethiopia", "+251", null),
        ("فيجي", "Fiji", "+679", null),
        ("فنلندا", "Finland", "+358", null),
        ("فرنسا", "France", "+33", null),
        ("الغابون", "Gabon", "+241", null),
        ("غامبيا", "Gambia", "+220", null),
        ("جورجيا", "Georgia", "+995", null),
        ("ألمانيا", "Germany", "+49", null),
        ("غانا", "Ghana", "+233", null),
        ("اليونان", "Greece", "+30", null),
        ("غرينادا", "Grenada", "+1-473", null),
        ("غواتيمالا", "Guatemala", "+502", null),
        ("غينيا", "Guinea", "+224", null),
        ("غينيا بيساو", "Guinea-Bissau", "+245", null),
        ("غيانا", "Guyana", "+592", null),
        ("هايتي", "Haiti", "+509", null),
        ("هندوراس", "Honduras", "+504", null),
        ("المجر", "Hungary", "+36", null),
        ("آيسلندا", "Iceland", "+354", null),
        ("الهند", "India", "+91", null),
        ("إندونيسيا", "Indonesia", "+62", null),
        ("إيران", "Iran", "+98", null),
        ("العراق", "Iraq", "+964", null),
        ("أيرلندا", "Ireland", "+353", null),
        ("إيطاليا", "Italy", "+39", null),
        ("ساحل العاج", "Ivory Coast", "+225", null),
        ("جامايكا", "Jamaica", "+1-876", null),
        ("اليابان", "Japan", "+81", null),
        ("الأردن", "Jordan", "+962", null),
        ("كازاخستان", "Kazakhstan", "+7", null),
        ("كينيا", "Kenya", "+254", null),
        ("كيريباتي", "Kiribati", "+686", null),
        ("كوريا الشمالية", "North Korea", "+850", null),
        ("كوريا الجنوبية", "South Korea", "+82", null),
        ("كوسوفو", "Kosovo", "+383", null),
        ("الكويت", "Kuwait", "+965", null),
        ("قيرغيزستان", "Kyrgyzstan", "+996", null),
        ("لاوس", "Laos", "+856", null),
        ("لاتفيا", "Latvia", "+371", null),
        ("لبنان", "Lebanon", "+961", null),
        ("ليسوتو", "Lesotho", "+266", null),
        ("ليبيريا", "Liberia", "+231", null),
        ("ليبيا", "Libya", "+218", null),
        ("ليختنشتاين", "Liechtenstein", "+423", null),
        ("ليتوانيا", "Lithuania", "+370", null),
        ("لوكسمبورغ", "Luxembourg", "+352", null),
        ("مدغشقر", "Madagascar", "+261", null),
        ("مالاوي", "Malawi", "+265", null),
        ("ماليزيا", "Malaysia", "+60", null),
        ("المالديف", "Maldives", "+960", null),
        ("مالي", "Mali", "+223", null),
        ("مالطا", "Malta", "+356", null),
        ("جزر مارشال", "Marshall Islands", "+692", null),
        ("موريتانيا", "Mauritania", "+222", null),
        ("موريشيوس", "Mauritius", "+230", null),
        ("المكسيك", "Mexico", "+52", null),
        ("ميكرونيزيا", "Micronesia", "+691", null),
        ("مولدوفا", "Moldova", "+373", null),
        ("موناكو", "Monaco", "+377", null),
        ("منغوليا", "Mongolia", "+976", null),
        ("الجبل الأسود", "Montenegro", "+382", null),
        ("المغرب", "Morocco", "+212", null),
        ("موزمبيق", "Mozambique", "+258", null),
        ("ميانمار", "Myanmar", "+95", null),
        ("ناميبيا", "Namibia", "+264", null),
        ("ناورو", "Nauru", "+674", null),
        ("نيبال", "Nepal", "+977", null),
        ("هولندا", "Netherlands", "+31", null),
        ("نيوزيلندا", "New Zealand", "+64", null),
        ("نيكاراغوا", "Nicaragua", "+505", null),
        ("النيجر", "Niger", "+227", null),
        ("نيجيريا", "Nigeria", "+234", null),
        ("مقدونيا الشمالية", "North Macedonia", "+389", null),
        ("النرويج", "Norway", "+47", null),
        ("عُمان", "Oman", "+968", null),
        ("باكستان", "Pakistan", "+92", null),
        ("بالاو", "Palau", "+680", null),
        ("فلسطين", "Palestine", "+970", null),
        ("بنما", "Panama", "+507", null),
        ("بابوا غينيا الجديدة", "Papua New Guinea", "+675", null),
        ("باراغواي", "Paraguay", "+595", null),
        ("بيرو", "Peru", "+51", null),
        ("الفلبين", "Philippines", "+63", null),
        ("بولندا", "Poland", "+48", null),
        ("البرتغال", "Portugal", "+351", null),
        ("قطر", "Qatar", "+974", null),
        ("رومانيا", "Romania", "+40", null),
        ("روسيا", "Russia", "+7", null),
        ("رواندا", "Rwanda", "+250", null),
        ("سانت كيتس ونيفيس", "Saint Kitts and Nevis", "+1-869", null),
        ("سانت لوسيا", "Saint Lucia", "+1-758", null),
        ("سانت فينسنت والغرينادين", "Saint Vincent and the Grenadines", "+1-784", null),
        ("ساموا", "Samoa", "+685", null),
        ("سان مارينو", "San Marino", "+378", null),
        ("ساو تومي وبرينسيبي", "Sao Tome and Principe", "+239", null),
        ("السعودية", "Saudi Arabia", "+966", null),
        ("السنغال", "Senegal", "+221", null),
        ("صربيا", "Serbia", "+381", null),
        ("سيشل", "Seychelles", "+248", null),
        ("سيراليون", "Sierra Leone", "+232", null),
        ("سنغافورة", "Singapore", "+65", null),
        ("سلوفاكيا", "Slovakia", "+421", null),
        ("سلوفينيا", "Slovenia", "+386", null),
        ("جزر سليمان", "Solomon Islands", "+677", null),
        ("الصومال", "Somalia", "+252", null),
        ("جنوب أفريقيا", "South Africa", "+27", null),
        ("جنوب السودان", "South Sudan", "+211", null),
        ("إسبانيا", "Spain", "+34", null),
        ("سريلانكا", "Sri Lanka", "+94", null),
        ("السودان", "Sudan", "+249", null),
        ("سورينام", "Suriname", "+597", null),
        ("السويد", "Sweden", "+46", null),
        ("سويسرا", "Switzerland", "+41", null),
        ("سوريا", "Syria", "+963", null),
        ("تايوان", "Taiwan", "+886", null),
        ("طاجيكستان", "Tajikistan", "+992", null),
        ("تنزانيا", "Tanzania", "+255", null),
        ("تايلاند", "Thailand", "+66", null),
        ("توغو", "Togo", "+228", null),
        ("تونغا", "Tonga", "+676", null),
        ("ترينيداد وتوباغو", "Trinidad and Tobago", "+1-868", null),
        ("تونس", "Tunisia", "+216", null),
        ("تركيا", "Turkey", "+90", null),
        ("تركمانستان", "Turkmenistan", "+993", null),
        ("توفالو", "Tuvalu", "+688", null),
        ("أوغندا", "Uganda", "+256", null),
        ("أوكرانيا", "Ukraine", "+380", null),
        ("الإمارات", "United Arab Emirates", "+971", null),
        ("المملكة المتحدة", "United Kingdom", "+44", null),
        ("الولايات المتحدة", "United States", "+1", null),
        ("أوروغواي", "Uruguay", "+598", null),
        ("أوزبكستان", "Uzbekistan", "+998", null),
        ("فانواتو", "Vanuatu", "+678", null),
        ("فنزويلا", "Venezuela", "+58", null),
        ("فيتنام", "Vietnam", "+84", null),
        ("اليمن", "Yemen", "+967", null),
        ("زامبيا", "Zambia", "+260", null),
        ("زيمبابوي", "Zimbabwe", "+263", null),
    };
}
