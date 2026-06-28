using System.Reflection;
using CCE.Application.Messages;
using CCE.Infrastructure.Localization;

namespace CCE.Application.Tests.Messages;

/// <summary>
/// Compile-time-equivalent safety net for the message pipeline.
/// A failing test here means a key will silently produce ERR900 or a raw key string
/// at runtime — i.e. a real client-visible bug.
/// </summary>
public sealed class SystemCodeMapIntegrityTests
{
    private static readonly LocalizationService Localization = new(new YamlLocalizationStore());

    private static readonly Dictionary<string, string> DomainToCode =
        (Dictionary<string, string>)typeof(SystemCodeMap)
            .GetField("DomainToCode", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;

    [Fact]
    public void Every_domain_key_has_Arabic_translation_in_Resources_yaml()
    {
        var missing = DomainToCode.Keys
            .Where(key =>
            {
                var value = Localization.GetString(key, "ar");
                return string.IsNullOrWhiteSpace(value) || value == key;
            })
            .OrderBy(k => k)
            .ToList();

        missing.Should().BeEmpty(
            because: "every SystemCodeMap domain key must have an Arabic translation in Resources.yaml; " +
                     "missing: {0}", string.Join(", ", missing));
    }

    [Fact]
    public void Every_domain_key_has_English_translation_in_Resources_yaml()
    {
        var missing = DomainToCode.Keys
            .Where(key =>
            {
                var value = Localization.GetString(key, "en");
                return string.IsNullOrWhiteSpace(value) || value == key;
            })
            .OrderBy(k => k)
            .ToList();

        missing.Should().BeEmpty(
            because: "every SystemCodeMap domain key must have an English translation in Resources.yaml; " +
                     "missing: {0}", string.Join(", ", missing));
    }

    [Fact]
    public void No_two_domain_keys_share_the_same_system_code()
    {
        var duplicates = DomainToCode
            .GroupBy(kv => kv.Value, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key} → [{string.Join(", ", g.Select(kv => kv.Key))}]")
            .OrderBy(s => s)
            .ToList();

        duplicates.Should().BeEmpty(
            because: "each domain key must map to a unique system code; duplicates: {0}",
            string.Join(" | ", duplicates));
    }

    [Fact]
    public void Every_SystemCode_constant_value_matches_its_field_name()
    {
        var mismatches = typeof(SystemCode)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => f.FieldType == typeof(string))
            .Where(f => (string)f.GetValue(null)! != f.Name)
            .Select(f => $"{f.Name} = \"{f.GetValue(null)}\"")
            .OrderBy(s => s)
            .ToList();

        mismatches.Should().BeEmpty(
            because: "a SystemCode constant's value must equal its field name to prevent copy-paste drift; " +
                     "mismatches: {0}", string.Join(", ", mismatches));
    }
}
