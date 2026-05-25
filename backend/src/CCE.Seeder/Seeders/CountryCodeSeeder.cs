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
            var entity = CountryCode.Create(name, c.DialCode, systemUser, _clock);
            typeof(CountryCode).GetProperty(nameof(entity.Id))!.SetValue(entity, id);
            _ctx.CountryCodes.Add(entity);
            seeded++;
        }

        await _ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "CountryCode seeder finished — seeded {Seeded}, skipped {Skipped}.", seeded, skipped);
    }

    // Data: (NameAr, NameEn, DialCode)
    // Israel is intentionally omitted. Palestine uses +972.
    private static readonly (string NameAr, string NameEn, string DialCode)[] CountryCodes =
    {
        ("أفغانستان", "Afghanistan", "+93"),
        ("ألبانيا", "Albania", "+355"),
        ("الجزائر", "Algeria", "+213"),
        ("أندورا", "Andorra", "+376"),
        ("أنغولا", "Angola", "+244"),
        ("أنتيغوا وبربودا", "Antigua and Barbuda", "+1-268"),
        ("الأرجنتين", "Argentina", "+54"),
        ("أرمينيا", "Armenia", "+374"),
        ("أستراليا", "Australia", "+61"),
        ("النمسا", "Austria", "+43"),
        ("أذربيجان", "Azerbaijan", "+994"),
        ("الباهاما", "Bahamas", "+1-242"),
        ("البحرين", "Bahrain", "+973"),
        ("بنغلاديش", "Bangladesh", "+880"),
        ("باربادوس", "Barbados", "+1-246"),
        ("بيلاروس", "Belarus", "+375"),
        ("بلجيكا", "Belgium", "+32"),
        ("بليز", "Belize", "+501"),
        ("بنين", "Benin", "+229"),
        ("بوتان", "Bhutan", "+975"),
        ("بوليفيا", "Bolivia", "+591"),
        ("البوسنة والهرسك", "Bosnia and Herzegovina", "+387"),
        ("بوتسوانا", "Botswana", "+267"),
        ("البرازيل", "Brazil", "+55"),
        ("بروناي", "Brunei", "+673"),
        ("بلغاريا", "Bulgaria", "+359"),
        ("بوركينا فاسو", "Burkina Faso", "+226"),
        ("بوروندي", "Burundi", "+257"),
        ("كابو فيردي", "Cabo Verde", "+238"),
        ("كمبوديا", "Cambodia", "+855"),
        ("الكاميرون", "Cameroon", "+237"),
        ("كندا", "Canada", "+1"),
        ("جمهورية أفريقيا الوسطى", "Central African Republic", "+236"),
        ("تشاد", "Chad", "+235"),
        ("تشيلي", "Chile", "+56"),
        ("الصين", "China", "+86"),
        ("كولومبيا", "Colombia", "+57"),
        ("جزر القمر", "Comoros", "+269"),
        ("الكونغو", "Congo", "+242"),
        ("الكونغو (الديمقراطية)", "Congo (DRC)", "+243"),
        ("كوستاريكا", "Costa Rica", "+506"),
        ("كرواتيا", "Croatia", "+385"),
        ("كوبا", "Cuba", "+53"),
        ("قبرص", "Cyprus", "+357"),
        ("التشيك", "Czech Republic", "+420"),
        ("الدانمرك", "Denmark", "+45"),
        ("جيبوتي", "Djibouti", "+253"),
        ("دومينيكا", "Dominica", "+1-767"),
        ("جمهورية الدومينيكان", "Dominican Republic", "+1-809"),
        ("تيمور الشرقية", "East Timor", "+670"),
        ("الإكوادور", "Ecuador", "+593"),
        ("مصر", "Egypt", "+20"),
        ("السلفادور", "El Salvador", "+503"),
        ("غينيا الاستوائية", "Equatorial Guinea", "+240"),
        ("إريتريا", "Eritrea", "+291"),
        ("إستونيا", "Estonia", "+372"),
        ("إسواتيني", "Eswatini", "+268"),
        ("إثيوبيا", "Ethiopia", "+251"),
        ("فيجي", "Fiji", "+679"),
        ("فنلندا", "Finland", "+358"),
        ("فرنسا", "France", "+33"),
        ("الغابون", "Gabon", "+241"),
        ("غامبيا", "Gambia", "+220"),
        ("جورجيا", "Georgia", "+995"),
        ("ألمانيا", "Germany", "+49"),
        ("غانا", "Ghana", "+233"),
        ("اليونان", "Greece", "+30"),
        ("غرينادا", "Grenada", "+1-473"),
        ("غواتيمالا", "Guatemala", "+502"),
        ("غينيا", "Guinea", "+224"),
        ("غينيا بيساو", "Guinea-Bissau", "+245"),
        ("غيانا", "Guyana", "+592"),
        ("هايتي", "Haiti", "+509"),
        ("هندوراس", "Honduras", "+504"),
        ("المجر", "Hungary", "+36"),
        ("آيسلندا", "Iceland", "+354"),
        ("الهند", "India", "+91"),
        ("إندونيسيا", "Indonesia", "+62"),
        ("إيران", "Iran", "+98"),
        ("العراق", "Iraq", "+964"),
        ("أيرلندا", "Ireland", "+353"),
        ("إيطاليا", "Italy", "+39"),
        ("ساحل العاج", "Ivory Coast", "+225"),
        ("جامايكا", "Jamaica", "+1-876"),
        ("اليابان", "Japan", "+81"),
        ("الأردن", "Jordan", "+962"),
        ("كازاخستان", "Kazakhstan", "+7"),
        ("كينيا", "Kenya", "+254"),
        ("كيريباتي", "Kiribati", "+686"),
        ("كوريا الشمالية", "North Korea", "+850"),
        ("كوريا الجنوبية", "South Korea", "+82"),
        ("كوسوفو", "Kosovo", "+383"),
        ("الكويت", "Kuwait", "+965"),
        ("قيرغيزستان", "Kyrgyzstan", "+996"),
        ("لاوس", "Laos", "+856"),
        ("لاتفيا", "Latvia", "+371"),
        ("لبنان", "Lebanon", "+961"),
        ("ليسوتو", "Lesotho", "+266"),
        ("ليبيريا", "Liberia", "+231"),
        ("ليبيا", "Libya", "+218"),
        ("ليختنشتاين", "Liechtenstein", "+423"),
        ("ليتوانيا", "Lithuania", "+370"),
        ("لوكسمبورغ", "Luxembourg", "+352"),
        ("مدغشقر", "Madagascar", "+261"),
        ("مالاوي", "Malawi", "+265"),
        ("ماليزيا", "Malaysia", "+60"),
        ("المالديف", "Maldives", "+960"),
        ("مالي", "Mali", "+223"),
        ("مالطا", "Malta", "+356"),
        ("جزر مارشال", "Marshall Islands", "+692"),
        ("موريتانيا", "Mauritania", "+222"),
        ("موريشيوس", "Mauritius", "+230"),
        ("المكسيك", "Mexico", "+52"),
        ("ميكرونيزيا", "Micronesia", "+691"),
        ("مولدوفا", "Moldova", "+373"),
        ("موناكو", "Monaco", "+377"),
        ("منغوليا", "Mongolia", "+976"),
        ("الجبل الأسود", "Montenegro", "+382"),
        ("المغرب", "Morocco", "+212"),
        ("موزمبيق", "Mozambique", "+258"),
        ("ميانمار", "Myanmar", "+95"),
        ("ناميبيا", "Namibia", "+264"),
        ("ناورو", "Nauru", "+674"),
        ("نيبال", "Nepal", "+977"),
        ("هولندا", "Netherlands", "+31"),
        ("نيوزيلندا", "New Zealand", "+64"),
        ("نيكاراغوا", "Nicaragua", "+505"),
        ("النيجر", "Niger", "+227"),
        ("نيجيريا", "Nigeria", "+234"),
        ("مقدونيا الشمالية", "North Macedonia", "+389"),
        ("النرويج", "Norway", "+47"),
        ("عُمان", "Oman", "+968"),
        ("باكستان", "Pakistan", "+92"),
        ("بالاو", "Palau", "+680"),
        ("فلسطين", "Palestine", "+970"),
        ("بنما", "Panama", "+507"),
        ("بابوا غينيا الجديدة", "Papua New Guinea", "+675"),
        ("باراغواي", "Paraguay", "+595"),
        ("بيرو", "Peru", "+51"),
        ("الفلبين", "Philippines", "+63"),
        ("بولندا", "Poland", "+48"),
        ("البرتغال", "Portugal", "+351"),
        ("قطر", "Qatar", "+974"),
        ("رومانيا", "Romania", "+40"),
        ("روسيا", "Russia", "+7"),
        ("رواندا", "Rwanda", "+250"),
        ("سانت كيتس ونيفيس", "Saint Kitts and Nevis", "+1-869"),
        ("سانت لوسيا", "Saint Lucia", "+1-758"),
        ("سانت فينسنت والغرينادين", "Saint Vincent and the Grenadines", "+1-784"),
        ("ساموا", "Samoa", "+685"),
        ("سان مارينو", "San Marino", "+378"),
        ("ساو تومي وبرينسيبي", "Sao Tome and Principe", "+239"),
        ("السعودية", "Saudi Arabia", "+966"),
        ("السنغال", "Senegal", "+221"),
        ("صربيا", "Serbia", "+381"),
        ("سيشل", "Seychelles", "+248"),
        ("سيراليون", "Sierra Leone", "+232"),
        ("سنغافورة", "Singapore", "+65"),
        ("سلوفاكيا", "Slovakia", "+421"),
        ("سلوفينيا", "Slovenia", "+386"),
        ("جزر سليمان", "Solomon Islands", "+677"),
        ("الصومال", "Somalia", "+252"),
        ("جنوب أفريقيا", "South Africa", "+27"),
        ("جنوب السودان", "South Sudan", "+211"),
        ("إسبانيا", "Spain", "+34"),
        ("سريلانكا", "Sri Lanka", "+94"),
        ("السودان", "Sudan", "+249"),
        ("سورينام", "Suriname", "+597"),
        ("السويد", "Sweden", "+46"),
        ("سويسرا", "Switzerland", "+41"),
        ("سوريا", "Syria", "+963"),
        ("تايوان", "Taiwan", "+886"),
        ("طاجيكستان", "Tajikistan", "+992"),
        ("تنزانيا", "Tanzania", "+255"),
        ("تايلاند", "Thailand", "+66"),
        ("توغو", "Togo", "+228"),
        ("تونغا", "Tonga", "+676"),
        ("ترينيداد وتوباغو", "Trinidad and Tobago", "+1-868"),
        ("تونس", "Tunisia", "+216"),
        ("تركيا", "Turkey", "+90"),
        ("تركمانستان", "Turkmenistan", "+993"),
        ("توفالو", "Tuvalu", "+688"),
        ("أوغندا", "Uganda", "+256"),
        ("أوكرانيا", "Ukraine", "+380"),
        ("الإمارات", "United Arab Emirates", "+971"),
        ("المملكة المتحدة", "United Kingdom", "+44"),
        ("الولايات المتحدة", "United States", "+1"),
        ("أوروغواي", "Uruguay", "+598"),
        ("أوزبكستان", "Uzbekistan", "+998"),
        ("فانواتو", "Vanuatu", "+678"),
        ("فنزويلا", "Venezuela", "+58"),
        ("فيتنام", "Vietnam", "+84"),
        ("اليمن", "Yemen", "+967"),
        ("زامبيا", "Zambia", "+260"),
        ("زيمبابوي", "Zimbabwe", "+263"),
    };
}
