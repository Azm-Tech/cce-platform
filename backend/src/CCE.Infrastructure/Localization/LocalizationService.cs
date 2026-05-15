using System.Globalization;
using CCE.Application.Localization;

namespace CCE.Infrastructure.Localization;

public sealed class LocalizationService : ILocalizationService
{
    private readonly YamlLocalizationStore _store;

    public LocalizationService(YamlLocalizationStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public string GetString(string key, string? culture = null)
    {
        var lang = GetTwoLetterCode(culture);

        if (string.IsNullOrWhiteSpace(key)) return string.Empty;
        if (_store.TryGet(key, out var language) && language != null)
        {
            if (language.TryGetValue(lang, out var v) && !string.IsNullOrEmpty(v)) return v;
            if (language.TryGetValue("ar", out var ar) && !string.IsNullOrEmpty(ar)) return ar;
            return language.Values.FirstOrDefault() ?? key;
        }

        return key;
    }

    public string GetStringOrDefault(string key, string defaultMessage, string? culture = null)
    {
        var v = GetString(key, culture);
        return string.IsNullOrEmpty(v) || v == key ? defaultMessage : v;
    }

    public LocalizedMessage GetLocalizedMessage(string key)
    {
        var enMessage = GetString(key, "en");
        var arMessage = GetString(key, "ar");

        if (string.IsNullOrEmpty(enMessage) || enMessage == key) enMessage = key;
        if (string.IsNullOrEmpty(arMessage) || arMessage == key) arMessage = key;

        return new LocalizedMessage(Ar: arMessage, En: enMessage);
    }

    private static string GetTwoLetterCode(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture)) return "ar";
        try
        {
            return new CultureInfo(culture).TwoLetterISOLanguageName;
        }
        catch (System.Globalization.CultureNotFoundException)
        {
            return "ar";
        }
    }
}
