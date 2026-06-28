using CCE.Domain.Common;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CCE.Seeder.Seeders;

/// <summary>
/// Idempotent seeder for world-country lookup entries in the <c>countries</c> table
/// (<c>is_cce_country = false</c>). Seeds all real-world countries with dial codes.
/// Israel is excluded; Palestine is included with the +970 dial code.
/// </summary>
public sealed class CountryCodeSeeder : ISeeder
{
    private readonly CceDbContext _ctx;
    private readonly ILogger<CountryCodeSeeder> _logger;

    public CountryCodeSeeder(CceDbContext ctx, ILogger<CountryCodeSeeder> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }

    public int Order => 25;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var seeded = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var c in CountryCodes)
        {
            var flagUrl = $"https://flagcdn.com/w640/{c.IsoAlpha2.ToLowerInvariant()}.png";

            // Look up by name (not ID) so the seeder is idempotent even when the
            // migration already copied country_codes rows with their old GUIDs.
            var existing = await _ctx.Countries.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => !x.IsCceCountry && x.NameEn == c.NameEn, cancellationToken)
                .ConfigureAwait(false);

            if (existing is not null)
            {
                if (existing.FlagUrl != flagUrl || existing.DialCode != c.DialCode)
                {
                    existing.UpdateLookup(existing.NameAr, existing.NameEn, c.DialCode, flagUrl, existing.IsActive);
                    updated++;
                }
                else
                {
                    skipped++;
                }
                continue;
            }

            var id = DeterministicGuid.From($"country_code:{c.NameEn}");
            var entity = CCE.Domain.Country.Country.RegisterLookup(
                c.NameAr, c.NameEn, c.DialCode, flagUrl, c.IsoAlpha2);

            // Force the deterministic GUID so future runs on fresh environments stay idempotent.
            typeof(CCE.Domain.Country.Country).GetProperty(nameof(entity.Id))!.SetValue(entity, id);
            _ctx.Countries.Add(entity);
            seeded++;
        }

        await _ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "CountryCode seeder finished — seeded {Seeded}, updated {Updated}, skipped {Skipped}.", seeded, updated, skipped);
    }

    // Data: (NameAr, NameEn, DialCode, IsoAlpha2)
    // Israel is intentionally omitted. Palestine uses +970.
    private static readonly (string NameAr, string NameEn, string DialCode, string IsoAlpha2)[] CountryCodes =
    {
        ("أفغانستان", "Afghanistan", "+93", "AF"),
        ("ألبانيا", "Albania", "+355", "AL"),
        ("الجزائر", "Algeria", "+213", "DZ"),
        ("أندورا", "Andorra", "+376", "AD"),
        ("أنغولا", "Angola", "+244", "AO"),
        ("أنتيغوا وبربودا", "Antigua and Barbuda", "+1-268", "AG"),
        ("الأرجنتين", "Argentina", "+54", "AR"),
        ("أرمينيا", "Armenia", "+374", "AM"),
        ("أستراليا", "Australia", "+61", "AU"),
        ("النمسا", "Austria", "+43", "AT"),
        ("أذربيجان", "Azerbaijan", "+994", "AZ"),
        ("الباهاما", "Bahamas", "+1-242", "BS"),
        ("البحرين", "Bahrain", "+973", "BH"),
        ("بنغلاديش", "Bangladesh", "+880", "BD"),
        ("باربادوس", "Barbados", "+1-246", "BB"),
        ("بيلاروس", "Belarus", "+375", "BY"),
        ("بلجيكا", "Belgium", "+32", "BE"),
        ("بليز", "Belize", "+501", "BZ"),
        ("بنين", "Benin", "+229", "BJ"),
        ("بوتان", "Bhutan", "+975", "BT"),
        ("بوليفيا", "Bolivia", "+591", "BO"),
        ("البوسنة والهرسك", "Bosnia and Herzegovina", "+387", "BA"),
        ("بوتسوانا", "Botswana", "+267", "BW"),
        ("البرازيل", "Brazil", "+55", "BR"),
        ("بروناي", "Brunei", "+673", "BN"),
        ("بلغاريا", "Bulgaria", "+359", "BG"),
        ("بوركينا فاسو", "Burkina Faso", "+226", "BF"),
        ("بوروندي", "Burundi", "+257", "BI"),
        ("كابو فيردي", "Cabo Verde", "+238", "CV"),
        ("كمبوديا", "Cambodia", "+855", "KH"),
        ("الكاميرون", "Cameroon", "+237", "CM"),
        ("كندا", "Canada", "+1", "CA"),
        ("جمهورية أفريقيا الوسطى", "Central African Republic", "+236", "CF"),
        ("تشاد", "Chad", "+235", "TD"),
        ("تشيلي", "Chile", "+56", "CL"),
        ("الصين", "China", "+86", "CN"),
        ("كولومبيا", "Colombia", "+57", "CO"),
        ("جزر القمر", "Comoros", "+269", "KM"),
        ("الكونغو", "Congo", "+242", "CG"),
        ("الكونغو (الديمقراطية)", "Congo (DRC)", "+243", "CD"),
        ("كوستاريكا", "Costa Rica", "+506", "CR"),
        ("كرواتيا", "Croatia", "+385", "HR"),
        ("كوبا", "Cuba", "+53", "CU"),
        ("قبرص", "Cyprus", "+357", "CY"),
        ("التشيك", "Czech Republic", "+420", "CZ"),
        ("الدانمرك", "Denmark", "+45", "DK"),
        ("جيبوتي", "Djibouti", "+253", "DJ"),
        ("دومينيكا", "Dominica", "+1-767", "DM"),
        ("جمهورية الدومينيكان", "Dominican Republic", "+1-809", "DO"),
        ("تيمور الشرقية", "East Timor", "+670", "TL"),
        ("الإكوادور", "Ecuador", "+593", "EC"),
        ("مصر", "Egypt", "+20", "EG"),
        ("السلفادور", "El Salvador", "+503", "SV"),
        ("غينيا الاستوائية", "Equatorial Guinea", "+240", "GQ"),
        ("إريتريا", "Eritrea", "+291", "ER"),
        ("إستونيا", "Estonia", "+372", "EE"),
        ("إسواتيني", "Eswatini", "+268", "SZ"),
        ("إثيوبيا", "Ethiopia", "+251", "ET"),
        ("فيجي", "Fiji", "+679", "FJ"),
        ("فنلندا", "Finland", "+358", "FI"),
        ("فرنسا", "France", "+33", "FR"),
        ("الغابون", "Gabon", "+241", "GA"),
        ("غامبيا", "Gambia", "+220", "GM"),
        ("جورجيا", "Georgia", "+995", "GE"),
        ("ألمانيا", "Germany", "+49", "DE"),
        ("غانا", "Ghana", "+233", "GH"),
        ("اليونان", "Greece", "+30", "GR"),
        ("غرينادا", "Grenada", "+1-473", "GD"),
        ("غواتيمالا", "Guatemala", "+502", "GT"),
        ("غينيا", "Guinea", "+224", "GN"),
        ("غينيا بيساو", "Guinea-Bissau", "+245", "GW"),
        ("غيانا", "Guyana", "+592", "GY"),
        ("هايتي", "Haiti", "+509", "HT"),
        ("هندوراس", "Honduras", "+504", "HN"),
        ("المجر", "Hungary", "+36", "HU"),
        ("آيسلندا", "Iceland", "+354", "IS"),
        ("الهند", "India", "+91", "IN"),
        ("إندونيسيا", "Indonesia", "+62", "ID"),
        ("إيران", "Iran", "+98", "IR"),
        ("العراق", "Iraq", "+964", "IQ"),
        ("أيرلندا", "Ireland", "+353", "IE"),
        ("إيطاليا", "Italy", "+39", "IT"),
        ("ساحل العاج", "Ivory Coast", "+225", "CI"),
        ("جامايكا", "Jamaica", "+1-876", "JM"),
        ("اليابان", "Japan", "+81", "JP"),
        ("الأردن", "Jordan", "+962", "JO"),
        ("كازاخستان", "Kazakhstan", "+7", "KZ"),
        ("كينيا", "Kenya", "+254", "KE"),
        ("كيريباتي", "Kiribati", "+686", "KI"),
        ("كوريا الشمالية", "North Korea", "+850", "KP"),
        ("كوريا الجنوبية", "South Korea", "+82", "KR"),
        ("كوسوفو", "Kosovo", "+383", "XK"),
        ("الكويت", "Kuwait", "+965", "KW"),
        ("قيرغيزستان", "Kyrgyzstan", "+996", "KG"),
        ("لاوس", "Laos", "+856", "LA"),
        ("لاتفيا", "Latvia", "+371", "LV"),
        ("لبنان", "Lebanon", "+961", "LB"),
        ("ليسوتو", "Lesotho", "+266", "LS"),
        ("ليبيريا", "Liberia", "+231", "LR"),
        ("ليبيا", "Libya", "+218", "LY"),
        ("ليختنشتاين", "Liechtenstein", "+423", "LI"),
        ("ليتوانيا", "Lithuania", "+370", "LT"),
        ("لوكسمبورغ", "Luxembourg", "+352", "LU"),
        ("مدغشقر", "Madagascar", "+261", "MG"),
        ("مالاوي", "Malawi", "+265", "MW"),
        ("ماليزيا", "Malaysia", "+60", "MY"),
        ("المالديف", "Maldives", "+960", "MV"),
        ("مالي", "Mali", "+223", "ML"),
        ("مالطا", "Malta", "+356", "MT"),
        ("جزر مارشال", "Marshall Islands", "+692", "MH"),
        ("موريتانيا", "Mauritania", "+222", "MR"),
        ("موريشيوس", "Mauritius", "+230", "MU"),
        ("المكسيك", "Mexico", "+52", "MX"),
        ("ميكرونيزيا", "Micronesia", "+691", "FM"),
        ("مولدوفا", "Moldova", "+373", "MD"),
        ("موناكو", "Monaco", "+377", "MC"),
        ("منغوليا", "Mongolia", "+976", "MN"),
        ("الجبل الأسود", "Montenegro", "+382", "ME"),
        ("المغرب", "Morocco", "+212", "MA"),
        ("موزمبيق", "Mozambique", "+258", "MZ"),
        ("ميانمار", "Myanmar", "+95", "MM"),
        ("ناميبيا", "Namibia", "+264", "NA"),
        ("ناورو", "Nauru", "+674", "NR"),
        ("نيبال", "Nepal", "+977", "NP"),
        ("هولندا", "Netherlands", "+31", "NL"),
        ("نيوزيلندا", "New Zealand", "+64", "NZ"),
        ("نيكاراغوا", "Nicaragua", "+505", "NI"),
        ("النيجر", "Niger", "+227", "NE"),
        ("نيجيريا", "Nigeria", "+234", "NG"),
        ("مقدونيا الشمالية", "North Macedonia", "+389", "MK"),
        ("النرويج", "Norway", "+47", "NO"),
        ("عُمان", "Oman", "+968", "OM"),
        ("باكستان", "Pakistan", "+92", "PK"),
        ("بالاو", "Palau", "+680", "PW"),
        ("فلسطين", "Palestine", "+970", "PS"),
        ("بنما", "Panama", "+507", "PA"),
        ("بابوا غينيا الجديدة", "Papua New Guinea", "+675", "PG"),
        ("باراغواي", "Paraguay", "+595", "PY"),
        ("بيرو", "Peru", "+51", "PE"),
        ("الفلبين", "Philippines", "+63", "PH"),
        ("بولندا", "Poland", "+48", "PL"),
        ("البرتغال", "Portugal", "+351", "PT"),
        ("قطر", "Qatar", "+974", "QA"),
        ("رومانيا", "Romania", "+40", "RO"),
        ("روسيا", "Russia", "+7", "RU"),
        ("رواندا", "Rwanda", "+250", "RW"),
        ("سانت كيتس ونيفيس", "Saint Kitts and Nevis", "+1-869", "KN"),
        ("سانت لوسيا", "Saint Lucia", "+1-758", "LC"),
        ("سانت فينسنت والغرينادين", "Saint Vincent and the Grenadines", "+1-784", "VC"),
        ("ساموا", "Samoa", "+685", "WS"),
        ("سان مارينو", "San Marino", "+378", "SM"),
        ("ساو تومي وبرينسيبي", "Sao Tome and Principe", "+239", "ST"),
        ("السعودية", "Saudi Arabia", "+966", "SA"),
        ("السنغال", "Senegal", "+221", "SN"),
        ("صربيا", "Serbia", "+381", "RS"),
        ("سيشل", "Seychelles", "+248", "SC"),
        ("سيراليون", "Sierra Leone", "+232", "SL"),
        ("سنغافورة", "Singapore", "+65", "SG"),
        ("سلوفاكيا", "Slovakia", "+421", "SK"),
        ("سلوفينيا", "Slovenia", "+386", "SI"),
        ("جزر سليمان", "Solomon Islands", "+677", "SB"),
        ("الصومال", "Somalia", "+252", "SO"),
        ("جنوب أفريقيا", "South Africa", "+27", "ZA"),
        ("جنوب السودان", "South Sudan", "+211", "SS"),
        ("إسبانيا", "Spain", "+34", "ES"),
        ("سريلانكا", "Sri Lanka", "+94", "LK"),
        ("السودان", "Sudan", "+249", "SD"),
        ("سورينام", "Suriname", "+597", "SR"),
        ("السويد", "Sweden", "+46", "SE"),
        ("سويسرا", "Switzerland", "+41", "CH"),
        ("سوريا", "Syria", "+963", "SY"),
        ("تايوان", "Taiwan", "+886", "TW"),
        ("طاجيكستان", "Tajikistan", "+992", "TJ"),
        ("تنزانيا", "Tanzania", "+255", "TZ"),
        ("تايلاند", "Thailand", "+66", "TH"),
        ("توغو", "Togo", "+228", "TG"),
        ("تونغا", "Tonga", "+676", "TO"),
        ("ترينيداد وتوباغو", "Trinidad and Tobago", "+1-868", "TT"),
        ("تونس", "Tunisia", "+216", "TN"),
        ("تركيا", "Turkey", "+90", "TR"),
        ("تركمانستان", "Turkmenistan", "+993", "TM"),
        ("توفالو", "Tuvalu", "+688", "TV"),
        ("أوغندا", "Uganda", "+256", "UG"),
        ("أوكرانيا", "Ukraine", "+380", "UA"),
        ("الإمارات", "United Arab Emirates", "+971", "AE"),
        ("المملكة المتحدة", "United Kingdom", "+44", "GB"),
        ("الولايات المتحدة", "United States", "+1", "US"),
        ("أوروغواي", "Uruguay", "+598", "UY"),
        ("أوزبكستان", "Uzbekistan", "+998", "UZ"),
        ("فانواتو", "Vanuatu", "+678", "VU"),
        ("فنزويلا", "Venezuela", "+58", "VE"),
        ("فيتنام", "Vietnam", "+84", "VN"),
        ("اليمن", "Yemen", "+967", "YE"),
        ("زامبيا", "Zambia", "+260", "ZM"),
        ("زيمبابوي", "Zimbabwe", "+263", "ZW"),
    };
}
