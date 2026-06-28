using System.Reflection;
using CCE.Application.Messages;

namespace CCE.ArchitectureTests;

/// <summary>
/// Safety net for the domain-key pipeline. Every constant in <see cref="MessageKeys"/>
/// must be present in <see cref="SystemCodeMap"/> (and vice‑versa) so no key silently
/// falls back to ERR900 at runtime.
/// </summary>
public sealed class DomainKeysIntegrityTests
{
    private static readonly Type[] DomainKeyClasses =
        typeof(MessageKeys).GetNestedTypes(BindingFlags.Public | BindingFlags.Static)
            .Where(t => t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Any(f => f.FieldType == typeof(string)))
            .ToArray();

    /// <summary>All public const string values declared in MessageKeys nested classes.</summary>
    private static readonly HashSet<string> DomainKeyValues = new(
        DomainKeyClasses.SelectMany(t => t
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)),
        StringComparer.OrdinalIgnoreCase);

    /// <summary>All keys in the SystemCodeMap dictionary.</summary>
    private static readonly HashSet<string> MappedKeys = new(
        GetSystemCodeMapKeys(), StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void Every_DomainKeys_constant_is_mapped_in_SystemCodeMap()
    {
        var unmapped = DomainKeyValues
            .Where(v => !MappedKeys.Contains(v))
            .OrderBy(k => k)
            .ToList();

        unmapped.Should().BeEmpty(
            because: "every MessageKeys constant must have a SystemCodeMap entry; " +
                     "unmapped: {0}", string.Join(", ", unmapped));
    }

    [Fact]
    public void Every_SystemCodeMap_key_has_a_corresponding_DomainKeys_constant()
    {
        var orphaned = MappedKeys
            .Where(k => !DomainKeyValues.Contains(k))
            .OrderBy(k => k)
            .ToList();

        orphaned.Should().BeEmpty(
            because: "every SystemCodeMap domain key must have a matching MessageKeys constant; " +
                     "orphaned: {0}", string.Join(", ", orphaned));
    }

    [Fact]
    public void No_DomainKeys_values_are_duplicated()
    {
        var duplicates = DomainKeyValues
            .GroupBy(k => k, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => $"'{g.Key}' appears {g.Count()} times")
            .OrderBy(s => s)
            .ToList();

        duplicates.Should().BeEmpty(
            because: "MessageKeys values must be unique across all categories to prevent " +
                     "ambiguous SystemCodeMap lookups; duplicates: {0}",
            string.Join(" | ", duplicates));
    }

    private static List<string> GetSystemCodeMapKeys()
    {
        var field = typeof(SystemCodeMap).GetField(
            "DomainToCode",
            BindingFlags.NonPublic | BindingFlags.Static);

        if (field?.GetValue(null) is Dictionary<string, string> dict)
            return dict.Keys.ToList();
        return [];
    }
}
