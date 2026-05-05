using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CCE.Domain.SourceGenerators;

/// <summary>
/// Roslyn incremental source generator that turns <c>backend/permissions.yaml</c> into:
/// (1) a strongly-typed <c>Permissions</c> static class with a constant per permission and an
/// <c>All</c> read-only list, and
/// (2) a <c>RolePermissionMap</c> static class with one read-only list per known role.
///
/// Two YAML schemas are supported:
/// - <strong>Flat</strong> (Foundation): top-level <c>permissions:</c> list; no role mappings.
///   In this mode, <c>RolePermissionMap</c> is still emitted but every role's collection is empty.
/// - <strong>Nested</strong> (sub-project 2): top-level <c>groups:</c> with arbitrary indented
///   sub-groups; each leaf has <c>description:</c> + <c>roles: [Role1, Role2]</c>.
///
/// The list of known roles is fixed at <see cref="KnownRoles"/> — adding a role requires a generator
/// change. This is intentional: roles are a domain-modelling decision, not a YAML-author decision.
/// </summary>
[Generator]
public sealed class PermissionsGenerator : IIncrementalGenerator
{
    private const string YamlFileName = "permissions.yaml";

    // Role values are the strings emitted in Entra ID app-role tokens (the
    // `roles` claim) and used in permissions.yaml's `roles:` lists. The
    // generator emits a corresponding C# property on RolePermissionMap whose
    // identifier is the value PascalCased via ToRoleMemberName (e.g.
    // "cce-admin" → "CceAdmin"). Phase 03 (Sub-11) renamed from Keycloak's
    // SuperAdmin-style names to Entra ID app-role values.
    private static readonly string[] KnownRoles =
    {
        "cce-admin",
        "cce-editor",
        "cce-reviewer",
        "cce-expert",
        "cce-user",
        "Anonymous",
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var yamlContent = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(YamlFileName, StringComparison.OrdinalIgnoreCase))
            .Select(static (file, ct) => file.GetText(ct)?.ToString() ?? string.Empty)
            .Collect();

        context.RegisterSourceOutput(yamlContent, static (spc, contents) =>
        {
            var yaml = contents.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)) ?? string.Empty;
            var entries = ParseYaml(yaml);
            var source = GenerateSource(entries);
            spc.AddSource("Permissions.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }

    /// <summary>One generated permission with its role mapping.</summary>
    internal readonly struct PermissionEntry
    {
        public PermissionEntry(string name, IReadOnlyList<string> roles)
        {
            Name = name;
            Roles = roles;
        }
        public string Name { get; }
        public IReadOnlyList<string> Roles { get; }
    }

    /// <summary>
    /// Returns parsed permissions in declaration order. Empty list if the YAML is empty,
    /// is whitespace-only, or starts with neither <c>groups:</c> nor <c>permissions:</c>.
    /// </summary>
    private static List<PermissionEntry> ParseYaml(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            return new List<PermissionEntry>();
        }

        var schema = DetectSchema(yaml);
        return schema switch
        {
            Schema.Flat => ParseFlatSchema(yaml),
            Schema.Nested => ParseNestedSchema(yaml),
            _ => new List<PermissionEntry>(),
        };
    }

    private enum Schema { Unknown, Flat, Nested }

    private static Schema DetectSchema(string yaml)
    {
        foreach (var raw in yaml.Split('\n'))
        {
            var line = raw.TrimEnd('\r').Trim();
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }
            if (line.StartsWith("groups:", StringComparison.Ordinal))
            {
                return Schema.Nested;
            }
            if (line.StartsWith("permissions:", StringComparison.Ordinal))
            {
                return Schema.Flat;
            }
            return Schema.Unknown;
        }
        return Schema.Unknown;
    }

    /// <summary>
    /// Foundation-format parser. Matches lines of the form <c>  - Some.Permission.Name</c>.
    /// Roles list is empty for flat-schema permissions.
    /// </summary>
    private static List<PermissionEntry> ParseFlatSchema(string yaml)
    {
        var result = new List<PermissionEntry>();
        var emptyRoles = Array.Empty<string>();
        foreach (var raw in yaml.Split('\n'))
        {
            var line = raw.TrimEnd('\r').Trim();
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }
            if (!line.StartsWith("- ", StringComparison.Ordinal))
            {
                continue;
            }
            var name = line.Substring(2).Trim().Trim('"', '\'').Trim();
            if (name.Length == 0 || !IsValidPermissionName(name))
            {
                continue;
            }
            result.Add(new PermissionEntry(name, emptyRoles));
        }
        return result;
    }

    /// <summary>
    /// Sub-project 2 nested-groups parser. Walks lines tracking indent depth; each leaf entry
    /// (one whose direct children include <c>description:</c> + <c>roles:</c>) emits a permission
    /// whose dotted name is the path of group keys from the root to the leaf key.
    /// </summary>
    private static List<PermissionEntry> ParseNestedSchema(string yaml)
    {
        var result = new List<PermissionEntry>();
        var lines = yaml.Split('\n');

        var stack = new List<(int Indent, string Key)>();
        string? pendingPermissionPath = null;
        List<string>? pendingRoles = null;
        bool sawDescription = false;

        void Flush()
        {
            if (pendingPermissionPath != null && pendingRoles != null && sawDescription)
            {
                result.Add(new PermissionEntry(pendingPermissionPath, pendingRoles));
            }
            pendingPermissionPath = null;
            pendingRoles = null;
            sawDescription = false;
        }

        for (int i = 0; i < lines.Length; i++)
        {
            var raw = lines[i].TrimEnd('\r');
            if (raw.Length == 0)
            {
                continue;
            }

            int indent = 0;
            while (indent < raw.Length && raw[indent] == ' ')
            {
                indent++;
            }

            var content = raw.Substring(indent);
            if (content.Length == 0 || content[0] == '#')
            {
                continue;
            }

            if (indent == 0 && content.StartsWith("groups:", StringComparison.Ordinal))
            {
                Flush();
                stack.Clear();
                continue;
            }

            if (content.StartsWith("description:", StringComparison.Ordinal))
            {
                sawDescription = true;
                continue;
            }

            if (content.StartsWith("roles:", StringComparison.Ordinal))
            {
                pendingRoles = ParseInlineRoles(content);
                Flush();
                continue;
            }

            if (!content.EndsWith(":", StringComparison.Ordinal))
            {
                continue;
            }
            var key = content.Substring(0, content.Length - 1).Trim();
            if (key.Length == 0)
            {
                continue;
            }

            while (stack.Count > 0 && stack[stack.Count - 1].Indent >= indent)
            {
                stack.RemoveAt(stack.Count - 1);
            }

            // If a previous pending permission was never completed (no roles line), drop it.
            // pendingRoles is always null here because the only assignment happens in the roles:
            // branch which calls Flush() before falling through to other line kinds.
            if (pendingPermissionPath != null)
            {
                pendingPermissionPath = null;
                sawDescription = false;
            }

            var pathParts = stack.Select(s => s.Key).Concat(new[] { key }).ToArray();
            var fullPath = string.Join(".", pathParts);

            pendingPermissionPath = IsValidPermissionName(fullPath) ? fullPath : null;
            pendingRoles = null;
            sawDescription = false;

            stack.Add((indent, key));
        }

        Flush();
        return result;
    }

    /// <summary>
    /// Parses the roles list from a line of the form <c>roles: [Role1, Role2]</c>.
    /// Returns an empty list for malformed/empty role lists.
    /// </summary>
    private static List<string> ParseInlineRoles(string content)
    {
        var result = new List<string>();
        var open = content.IndexOf('[');
        var close = content.LastIndexOf(']');
        if (open < 0 || close < 0 || close <= open)
        {
            return result;
        }
        var inside = content.Substring(open + 1, close - open - 1);
        foreach (var part in inside.Split(','))
        {
            var role = part.Trim().Trim('"', '\'').Trim();
            if (role.Length > 0)
            {
                result.Add(role);
            }
        }
        return result;
    }

    private static bool IsValidPermissionName(string name)
    {
        var segments = name.Split('.');
        if (segments.Length < 2)
        {
            return false;
        }
        foreach (var seg in segments)
        {
            if (seg.Length == 0 || !char.IsUpper(seg[0]))
            {
                return false;
            }
            for (var i = 1; i < seg.Length; i++)
            {
                if (!char.IsLetterOrDigit(seg[i]))
                {
                    return false;
                }
            }
        }
        return true;
    }

    private static string GenerateSource(List<PermissionEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated>");
        sb.AppendLine("//   Generated by CCE.Domain.SourceGenerators.PermissionsGenerator from permissions.yaml.");
        sb.AppendLine("//   DO NOT EDIT — change the YAML and rebuild.");
        sb.AppendLine("// </auto-generated>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        sb.AppendLine("namespace CCE.Domain;");
        sb.AppendLine();

        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Strongly-typed permission name constants generated from <c>permissions.yaml</c>.");
        sb.AppendLine("/// Use these constants in policy registrations and <c>[RequirePermission(...)]</c> attributes.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static class Permissions");
        sb.AppendLine("{");
        foreach (var e in entries)
        {
            var memberName = ToMemberName(e.Name);
            sb.AppendLine($"    /// <summary>The <c>{e.Name}</c> permission.</summary>");
            sb.AppendLine($"    public const string {memberName} = \"{e.Name}\";");
            sb.AppendLine();
        }
        sb.AppendLine("    /// <summary>Every permission, in YAML declaration order.</summary>");
        if (entries.Count == 0)
        {
            sb.AppendLine("    public static IReadOnlyList<string> All { get; } = System.Array.Empty<string>();");
        }
        else
        {
            sb.AppendLine("    public static IReadOnlyList<string> All { get; } = new[]");
            sb.AppendLine("    {");
            foreach (var e in entries)
            {
                sb.AppendLine($"        {ToMemberName(e.Name)},");
            }
            sb.AppendLine("    };");
        }
        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Maps each role to the permissions assigned to it in <c>permissions.yaml</c>.");
        sb.AppendLine("/// One <see cref=\"IReadOnlyList{T}\"/> per known role; empty when the role has no assignments.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static class RolePermissionMap");
        sb.AppendLine("{");
        for (int r = 0; r < KnownRoles.Length; r++)
        {
            var role = KnownRoles[r];
            var memberName = ToRoleMemberName(role);
            var matches = entries.Where(e => e.Roles.Contains(role)).Select(e => e.Name).ToArray();
            sb.AppendLine($"    /// <summary>Permissions assigned to the <c>{role}</c> role.</summary>");
            if (matches.Length == 0)
            {
                sb.AppendLine($"    public static IReadOnlyList<string> {memberName} {{ get; }} = System.Array.Empty<string>();");
            }
            else
            {
                sb.AppendLine($"    public static IReadOnlyList<string> {memberName} {{ get; }} = new[]");
                sb.AppendLine("    {");
                foreach (var name in matches)
                {
                    sb.AppendLine($"        \"{name}\",");
                }
                sb.AppendLine("    };");
            }
            if (r < KnownRoles.Length - 1)
            {
                sb.AppendLine();
            }
        }
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string ToMemberName(string permission) => permission.Replace('.', '_');

    /// <summary>
    /// Converts a role value (e.g. "cce-admin") into a valid C# member
    /// identifier (e.g. "CceAdmin") for use as a property name on the
    /// generated RolePermissionMap. Splits on '-', uppercases each segment's
    /// first character, joins. Leaves identifier-safe inputs (e.g. "Anonymous")
    /// unchanged in shape.
    /// </summary>
    private static string ToRoleMemberName(string role)
    {
        if (string.IsNullOrEmpty(role)) return role;
        var parts = role.Split('-');
        var sb = new StringBuilder(role.Length);
        foreach (var part in parts)
        {
            if (part.Length == 0) continue;
            sb.Append(char.ToUpperInvariant(part[0]));
            if (part.Length > 1) sb.Append(part.Substring(1));
        }
        return sb.ToString();
    }
}
