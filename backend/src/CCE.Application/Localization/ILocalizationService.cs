namespace CCE.Application.Localization;

public interface ILocalizationService
{
    string GetString(string key, string? culture = null);
    string GetStringOrDefault(string key, string defaultMessage, string? culture = null);
    LocalizedMessage GetLocalizedMessage(string key);
}
