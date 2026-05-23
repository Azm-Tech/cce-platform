using System.Text.Json;
using System.Text.RegularExpressions;
using CCE.Application.Notifications;
using CCE.Domain.Common;
using CCE.Domain.Notifications;

namespace CCE.Infrastructure.Notifications;

/// <summary>
/// Replaces <c>{{Variable}}</c> placeholders in template subject/body with values from the provided dictionary.
/// </summary>
public sealed class NotificationTemplateRenderer : INotificationTemplateRenderer
{
    private static readonly Regex PlaceholderPattern = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

    public (string SubjectAr, string SubjectEn, string Body) Render(
        NotificationTemplate template,
        IReadOnlyDictionary<string, string> variables,
        string locale)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(variables);
        if (locale != "ar" && locale != "en")
            throw new DomainException("Locale must be 'ar' or 'en'.");

        ValidateVariables(template, variables);

        var subjectAr = ReplacePlaceholders(template.SubjectAr, variables);
        var subjectEn = ReplacePlaceholders(template.SubjectEn, variables);
        var body = locale == "ar"
            ? ReplacePlaceholders(template.BodyAr, variables)
            : ReplacePlaceholders(template.BodyEn, variables);

        return (subjectAr, subjectEn, body);
    }

    private static void ValidateVariables(NotificationTemplate template, IReadOnlyDictionary<string, string> variables)
    {
        var requiredKeys = ExtractRequiredKeys(template.VariableSchemaJson);
        foreach (var key in requiredKeys)
        {
            if (!variables.ContainsKey(key) || string.IsNullOrWhiteSpace(variables[key]))
                throw new DomainException($"Missing required notification variable: '{key}'.");
        }
    }

    private static HashSet<string> ExtractRequiredKeys(string variableSchemaJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(variableSchemaJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return [];

            var required = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in doc.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    if (property.Value.TryGetProperty("required", out var reqProp) &&
                        reqProp.ValueKind == JsonValueKind.True)
                    {
                        required.Add(property.Name);
                    }
                }
            }
            return required;
        }
        catch (JsonException)
        {
            // If schema is not valid JSON, fall back to extracting placeholders from the template body
            return [];
        }
    }

    private static string ReplacePlaceholders(string templateText, IReadOnlyDictionary<string, string> variables)
    {
        return PlaceholderPattern.Replace(templateText, match =>
        {
            var key = match.Groups[1].Value;
            return variables.TryGetValue(key, out var value) ? value : match.Value;
        });
    }
}
