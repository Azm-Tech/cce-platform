# Phase 01 — Permissions YAML expansion + source-generator extension

> Parent: [`../2026-04-27-data-domain.md`](../2026-04-27-data-domain.md) · Spec: [`../../specs/2026-04-27-data-domain-design.md`](../../specs/2026-04-27-data-domain-design.md) §3.3 + §5.6

**Phase goal:** Extend the existing `PermissionsGenerator` (Roslyn `IIncrementalGenerator`) to (a) parse the nested `groups:` schema, (b) emit a new `RolePermissionMap` static class with one collection per role, and (c) replace `backend/permissions.yaml` with the full BRD §4.1.31 matrix (41 permissions across 6 roles). Foundation's flat-list schema must keep parsing — the generator detects schema and adapts.

**Tasks in this phase:** 6
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 00 closed (commit `64508eb` is HEAD).
- `dotnet build backend/CCE.sln --no-restore` 0 warnings 0 errors.
- `dotnet test backend/CCE.sln --no-build` reports 71 backend passing.
- `backend/permissions.yaml` is the Foundation single-permission file.
- `backend/src/CCE.Domain.SourceGenerators/PermissionsGenerator.cs` is the Foundation flat-list generator.

---

## Pre-execution sanity checks

1. `git rev-parse HEAD` → `64508eb...` (or later) and tree clean (`.claude/` untracked is OK).
2. `grep '<Version>' backend/Directory.Packages.props | grep -E 'Microsoft\.CodeAnalysis\.CSharp|YamlDotNet'` → both present (Microsoft.CodeAnalysis.CSharp 4.8.0 + YamlDotNet 16.2.0 already pinned). YamlDotNet is **not** consumed by this phase — we keep the hand-rolled parser to avoid bundling a third-party assembly into the analyzer host. The pin stays for future reuse.
3. `wc -l backend/permissions.yaml` → 16 lines (Foundation seed).
4. `cat backend/permissions.yaml | grep -c '^- '` → 0 (top-level list is under `permissions:` indent — sanity only).

If any fail, stop and report.

---

## Task 1.1: Bootstrap `CCE.Domain.SourceGenerators.Tests` project + smoke test

**Files:**
- Create: `backend/tests/CCE.Domain.SourceGenerators.Tests/CCE.Domain.SourceGenerators.Tests.csproj`
- Create: `backend/tests/CCE.Domain.SourceGenerators.Tests/GlobalUsings.cs`
- Create: `backend/tests/CCE.Domain.SourceGenerators.Tests/GeneratorTestHarness.cs`
- Create: `backend/tests/CCE.Domain.SourceGenerators.Tests/SmokeGeneratorTests.cs`
- Modify: `backend/CCE.sln` (add the new project)

**Rationale:** We need a focused test project that drives the source generator via `CSharpGeneratorDriver` and inspects the generated output. Tasks 1.2 and 1.3 will add nested-schema + RolePermissionMap tests against the same harness. Keeping the harness in a separate file lets every test file read at a glance.

- [ ] **Step 1: Create the csproj**

`backend/tests/CCE.Domain.SourceGenerators.Tests/CCE.Domain.SourceGenerators.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>12.0</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <!-- Reference the generator as a normal library (NOT as an analyzer) so we can
         instantiate it directly and drive it with CSharpGeneratorDriver. -->
    <ProjectReference Include="..\..\src\CCE.Domain.SourceGenerators\CCE.Domain.SourceGenerators.csproj"
                      ReferenceOutputAssembly="true" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="coverlet.collector">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>

    <!-- Roslyn host APIs needed to drive the generator. Versions inherit from CPM. -->
    <PackageReference Include="Microsoft.CodeAnalysis.Common" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Add GlobalUsings**

`backend/tests/CCE.Domain.SourceGenerators.Tests/GlobalUsings.cs`:

```csharp
global using FluentAssertions;
global using Xunit;
```

- [ ] **Step 3: Add the test harness**

`backend/tests/CCE.Domain.SourceGenerators.Tests/GeneratorTestHarness.cs`:

```csharp
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using CCE.Domain.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace CCE.Domain.SourceGenerators.Tests;

/// <summary>
/// Drives the <see cref="PermissionsGenerator"/> against an in-memory <c>permissions.yaml</c> string and
/// returns the generated <c>Permissions.g.cs</c> source text. Use <see cref="Run"/> in tests, then assert
/// against the returned string.
/// </summary>
internal static class GeneratorTestHarness
{
    public static string Run(string yaml)
    {
        var generator = new PermissionsGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTest",
            syntaxTrees: null,
            references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var additional = new InMemoryAdditionalText("permissions.yaml", yaml);
        driver = (CSharpGeneratorDriver)driver.AddAdditionalTexts(
            ImmutableArray.Create<AdditionalText>(additional));

        var runResult = driver.RunGenerators(compilation).GetRunResult();

        // The generator is expected to emit exactly one source file (Permissions.g.cs).
        // If it emits zero (empty YAML edge case), return empty string so tests can assert that explicitly.
        var generated = runResult.Results.SelectMany(r => r.GeneratedSources).FirstOrDefault();
        return generated.SourceText?.ToString() ?? string.Empty;
    }

    private sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly string _content;

        public InMemoryAdditionalText(string path, string content)
        {
            Path = path;
            _content = content;
        }

        public override string Path { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default)
            => SourceText.From(_content, Encoding.UTF8);
    }
}
```

- [ ] **Step 4: Add the smoke test (asserts Foundation flat schema still works)**

`backend/tests/CCE.Domain.SourceGenerators.Tests/SmokeGeneratorTests.cs`:

```csharp
namespace CCE.Domain.SourceGenerators.Tests;

public class SmokeGeneratorTests
{
    [Fact]
    public void Flat_schema_with_one_permission_emits_constant_and_All_collection()
    {
        const string yaml = """
            permissions:
              - System.Health.Read
            """;

        var generated = GeneratorTestHarness.Run(yaml);

        generated.Should().Contain("public const string System_Health_Read = \"System.Health.Read\";");
        generated.Should().Contain("public static IReadOnlyList<string> All { get; }");
        generated.Should().Contain("System_Health_Read,");
    }

    [Fact]
    public void Empty_yaml_still_emits_a_compilable_Permissions_class()
    {
        var generated = GeneratorTestHarness.Run(string.Empty);

        generated.Should().Contain("public static class Permissions");
        generated.Should().Contain("public static IReadOnlyList<string> All { get; }");
    }
}
```

- [ ] **Step 5: Add the test project to the solution**

Run:

```bash
dotnet sln backend/CCE.sln add backend/tests/CCE.Domain.SourceGenerators.Tests/CCE.Domain.SourceGenerators.Tests.csproj
```

Expected: `Project ... added to the solution.`

- [ ] **Step 6: Build + run the smoke tests**

Run:

```bash
dotnet build backend/tests/CCE.Domain.SourceGenerators.Tests/CCE.Domain.SourceGenerators.Tests.csproj --nologo --no-restore 2>&1 | tail -8
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`. (If NuGet feed errors during restore, run with `--no-restore` after a successful `dotnet restore`.)

Run:

```bash
dotnet test backend/tests/CCE.Domain.SourceGenerators.Tests/CCE.Domain.SourceGenerators.Tests.csproj --nologo --no-build --logger "console;verbosity=minimal" 2>&1 | tail -8
```

Expected: `Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2`.

- [ ] **Step 7: Commit**

```bash
git add backend/tests/CCE.Domain.SourceGenerators.Tests/CCE.Domain.SourceGenerators.Tests.csproj backend/tests/CCE.Domain.SourceGenerators.Tests/GlobalUsings.cs backend/tests/CCE.Domain.SourceGenerators.Tests/GeneratorTestHarness.cs backend/tests/CCE.Domain.SourceGenerators.Tests/SmokeGeneratorTests.cs backend/CCE.sln
git -c commit.gpgsign=false commit -m "test(sourcegen): bootstrap CCE.Domain.SourceGenerators.Tests with smoke + Foundation-flat regression (2 tests)"
```

---

## Task 1.2: Failing tests for nested-`groups:` schema parsing

**Files:**
- Create: `backend/tests/CCE.Domain.SourceGenerators.Tests/NestedSchemaGeneratorTests.cs`

**Rationale:** Drive the generator extension with concrete examples of the nested schema. After this task, 4 new tests fail (RED) — Task 1.4 turns them green.

- [ ] **Step 1: Write the failing tests**

`backend/tests/CCE.Domain.SourceGenerators.Tests/NestedSchemaGeneratorTests.cs`:

```csharp
namespace CCE.Domain.SourceGenerators.Tests;

public class NestedSchemaGeneratorTests
{
    [Fact]
    public void Two_level_nested_yields_dotted_constant()
    {
        const string yaml = """
            groups:
              User:
                Read:
                  description: Read user profiles
                  roles: [SuperAdmin]
            """;

        var generated = GeneratorTestHarness.Run(yaml);

        generated.Should().Contain("public const string User_Read = \"User.Read\";");
        generated.Should().Contain("User_Read,");
    }

    [Fact]
    public void Three_level_nested_yields_three_segment_constant()
    {
        const string yaml = """
            groups:
              Resource:
                Center:
                  Upload:
                    description: Upload a center-managed resource
                    roles: [SuperAdmin, ContentManager]
            """;

        var generated = GeneratorTestHarness.Run(yaml);

        generated.Should().Contain("public const string Resource_Center_Upload = \"Resource.Center.Upload\";");
    }

    [Fact]
    public void Multiple_top_level_groups_each_emit_their_permissions()
    {
        const string yaml = """
            groups:
              User:
                Read:
                  description: Read users
                  roles: [SuperAdmin]
                Create:
                  description: Create users
                  roles: [SuperAdmin]
              Page:
                Edit:
                  description: Edit pages
                  roles: [SuperAdmin, ContentManager]
            """;

        var generated = GeneratorTestHarness.Run(yaml);

        generated.Should().Contain("User_Read = \"User.Read\"");
        generated.Should().Contain("User_Create = \"User.Create\"");
        generated.Should().Contain("Page_Edit = \"Page.Edit\"");
    }

    [Fact]
    public void Comments_and_blank_lines_are_ignored()
    {
        const string yaml = """
            # Header comment
            groups:
              # mid comment
              User:

                Read:
                  description: x
                  roles: [SuperAdmin]
            """;

        var generated = GeneratorTestHarness.Run(yaml);

        generated.Should().Contain("User_Read = \"User.Read\"");
    }
}
```

- [ ] **Step 2: Run — expect 4 failures**

Run:

```bash
dotnet test backend/tests/CCE.Domain.SourceGenerators.Tests/CCE.Domain.SourceGenerators.Tests.csproj --nologo --no-build --filter "FullyQualifiedName~NestedSchemaGeneratorTests" --logger "console;verbosity=minimal" 2>&1 | tail -12
```

Expected: `Failed:     4` (the generator currently doesn't recognize `groups:` so no `User_Read` etc. constants are emitted; the smoke + previous tests still pass).

If the count is anything other than 4 failed / 0 passed, **stop** and re-read this task.

- [ ] **Step 3: Commit the failing tests (RED)**

We commit RED tests intentionally so the generator change in Task 1.4 has a target to satisfy.

```bash
git add backend/tests/CCE.Domain.SourceGenerators.Tests/NestedSchemaGeneratorTests.cs
git -c commit.gpgsign=false commit -m "test(sourcegen): RED — 4 failing nested-groups schema tests for PermissionsGenerator"
```

---

## Task 1.3: Failing tests for `RolePermissionMap` emission

**Files:**
- Create: `backend/tests/CCE.Domain.SourceGenerators.Tests/RolePermissionMapGeneratorTests.cs`

**Rationale:** A second batch of RED tests covering the role mapping output. Task 1.4 turns these green together with the nested-schema tests.

- [ ] **Step 1: Write the failing tests**

`backend/tests/CCE.Domain.SourceGenerators.Tests/RolePermissionMapGeneratorTests.cs`:

```csharp
namespace CCE.Domain.SourceGenerators.Tests;

public class RolePermissionMapGeneratorTests
{
    [Fact]
    public void RolePermissionMap_class_is_emitted()
    {
        const string yaml = """
            groups:
              User:
                Read:
                  description: x
                  roles: [SuperAdmin]
            """;

        var generated = GeneratorTestHarness.Run(yaml);

        generated.Should().Contain("public static class RolePermissionMap");
    }

    [Fact]
    public void Role_with_one_permission_emits_single_entry_collection()
    {
        const string yaml = """
            groups:
              User:
                Read:
                  description: x
                  roles: [SuperAdmin]
            """;

        var generated = GeneratorTestHarness.Run(yaml);

        generated.Should().Contain("public static IReadOnlyList<string> SuperAdmin { get; } = new[]");
        generated.Should().Contain("\"User.Read\",");
    }

    [Fact]
    public void Permission_assigned_to_multiple_roles_appears_in_each_role_collection()
    {
        const string yaml = """
            groups:
              Page:
                Edit:
                  description: x
                  roles: [SuperAdmin, ContentManager]
            """;

        var generated = GeneratorTestHarness.Run(yaml);

        // Both role collections must contain "Page.Edit"
        var superAdminBlock = ExtractRoleBlock(generated, "SuperAdmin");
        var contentManagerBlock = ExtractRoleBlock(generated, "ContentManager");

        superAdminBlock.Should().Contain("\"Page.Edit\"");
        contentManagerBlock.Should().Contain("\"Page.Edit\"");
    }

    [Fact]
    public void All_six_roles_are_emitted_even_when_some_have_no_permissions()
    {
        // Only SuperAdmin has a permission. The generator must still emit empty collections for the other five.
        const string yaml = """
            groups:
              User:
                Delete:
                  description: x
                  roles: [SuperAdmin]
            """;

        var generated = GeneratorTestHarness.Run(yaml);

        generated.Should().Contain("public static IReadOnlyList<string> SuperAdmin { get; }");
        generated.Should().Contain("public static IReadOnlyList<string> ContentManager { get; }");
        generated.Should().Contain("public static IReadOnlyList<string> StateRepresentative { get; }");
        generated.Should().Contain("public static IReadOnlyList<string> CommunityExpert { get; }");
        generated.Should().Contain("public static IReadOnlyList<string> RegisteredUser { get; }");
        generated.Should().Contain("public static IReadOnlyList<string> Anonymous { get; }");
    }

    /// <summary>
    /// Returns the substring of <paramref name="generated"/> covering the body of the named role's
    /// collection, e.g. for role "SuperAdmin": everything between
    /// "public static IReadOnlyList&lt;string&gt; SuperAdmin { get; } = new[]" and the next "};".
    /// </summary>
    private static string ExtractRoleBlock(string generated, string roleName)
    {
        var marker = $"IReadOnlyList<string> {roleName} {{ get; }} = new[]";
        var start = generated.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            return string.Empty;
        }
        var end = generated.IndexOf("};", start, StringComparison.Ordinal);
        return end < 0 ? generated.Substring(start) : generated.Substring(start, end - start);
    }
}
```

- [ ] **Step 2: Run — expect 4 failures**

Run:

```bash
dotnet test backend/tests/CCE.Domain.SourceGenerators.Tests/CCE.Domain.SourceGenerators.Tests.csproj --nologo --no-build --filter "FullyQualifiedName~RolePermissionMapGeneratorTests" --logger "console;verbosity=minimal" 2>&1 | tail -12
```

Expected: `Failed:     4` (no RolePermissionMap class emitted yet).

- [ ] **Step 3: Commit the failing tests (RED)**

```bash
git add backend/tests/CCE.Domain.SourceGenerators.Tests/RolePermissionMapGeneratorTests.cs
git -c commit.gpgsign=false commit -m "test(sourcegen): RED — 4 failing RolePermissionMap emission tests for PermissionsGenerator"
```

---

## Task 1.4: Implement nested-`groups:` parser + `RolePermissionMap` emitter (GREEN)

**Files:**
- Modify: `backend/src/CCE.Domain.SourceGenerators/PermissionsGenerator.cs` (full rewrite — keep flat-schema parser path for backward compat)

**Rationale:** Single change that turns all 8 RED tests green. The implementation:

1. **Schema detection:** scan first non-comment, non-blank line. If it starts with `groups:`, run the nested parser. If it starts with `permissions:`, run the legacy flat parser. Else, treat as empty.
2. **Nested parser:** indent-based stack. Each entry whose immediate children include both `description:` and `roles:` is a leaf permission whose dotted name is the join of the stack path. The `roles: [...]` value is parsed as a comma-separated list of role names.
3. **Emitter:** emits the same `Permissions` static class as before AND a new `RolePermissionMap` static class with one collection per role from a fixed `KnownRoles` array (so empty roles still emit empty collections).

The 6 known roles are hard-coded — they're a domain decision per spec §5.6, not parsed from the YAML. Adding a new role = source-generator change, by design.

- [ ] **Step 1: Replace the generator file with the extended implementation**

`backend/src/CCE.Domain.SourceGenerators/PermissionsGenerator.cs`:

```csharp
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

    private static readonly string[] KnownRoles =
    {
        "SuperAdmin",
        "ContentManager",
        "StateRepresentative",
        "CommunityExpert",
        "RegisteredUser",
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

        // Stack entries: (indentSpaces, key). Topmost = innermost.
        var stack = new List<(int Indent, string Key)>();
        // Pending: the most recent key we saw that hasn't been confirmed as a permission yet.
        // It becomes a permission when we encounter its `description:` AND `roles:` children.
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

            // Count leading spaces (indent).
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

            // Top-level "groups:" header — reset stack and skip.
            if (indent == 0 && content.StartsWith("groups:", StringComparison.Ordinal))
            {
                Flush();
                stack.Clear();
                continue;
            }

            // "description: ..." — marks the current pending entry as a permission candidate.
            if (content.StartsWith("description:", StringComparison.Ordinal))
            {
                sawDescription = true;
                continue;
            }

            // "roles: [Role1, Role2]" — parse the list and complete the pending entry.
            if (content.StartsWith("roles:", StringComparison.Ordinal))
            {
                pendingRoles = ParseInlineRoles(content);
                Flush();
                continue;
            }

            // Otherwise: it's a key opening a sub-group or a leaf entry.
            // Format: "Key:" (with optional trailing whitespace).
            if (!content.EndsWith(":", StringComparison.Ordinal))
            {
                continue;
            }
            var key = content.Substring(0, content.Length - 1).Trim();
            if (key.Length == 0)
            {
                continue;
            }

            // Pop stack to current indent depth.
            while (stack.Count > 0 && stack[stack.Count - 1].Indent >= indent)
            {
                stack.RemoveAt(stack.Count - 1);
            }

            // If a previous pending permission was never completed (no roles line), drop it.
            if (pendingPermissionPath != null && (pendingRoles == null || !sawDescription))
            {
                pendingPermissionPath = null;
                pendingRoles = null;
                sawDescription = false;
            }

            // The full path is stack-keys + this key.
            var pathParts = stack.Select(s => s.Key).Concat(new[] { key }).ToArray();
            var fullPath = string.Join(".", pathParts);

            // Treat this as a candidate permission. If its children turn out to NOT include
            // description+roles, the next sibling key will discard the candidate (above).
            pendingPermissionPath = IsValidPermissionName(fullPath) ? fullPath : null;
            pendingRoles = null;
            sawDescription = false;

            // Push onto the stack so deeper lines see this key as their parent.
            stack.Add((indent, key));
        }

        // Flush any final pending entry (e.g. file ends after a roles: line was processed).
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

        // Permissions class
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
        sb.AppendLine("    public static IReadOnlyList<string> All { get; } = new[]");
        sb.AppendLine("    {");
        foreach (var e in entries)
        {
            sb.AppendLine($"        {ToMemberName(e.Name)},");
        }
        sb.AppendLine("    };");
        sb.AppendLine("}");
        sb.AppendLine();

        // RolePermissionMap class — one collection per known role. Empty roles still emit
        // an empty array literal (Array.Empty would require a different syntax tree).
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Maps each role to the permissions assigned to it in <c>permissions.yaml</c>.");
        sb.AppendLine("/// One <see cref=\"IReadOnlyList{T}\"/> per known role; empty when the role has no assignments.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static class RolePermissionMap");
        sb.AppendLine("{");
        for (int r = 0; r < KnownRoles.Length; r++)
        {
            var role = KnownRoles[r];
            var matches = entries.Where(e => e.Roles.Contains(role)).Select(e => e.Name).ToArray();
            sb.AppendLine($"    /// <summary>Permissions assigned to the <c>{role}</c> role.</summary>");
            sb.Append($"    public static IReadOnlyList<string> {role} {{ get; }} = new[]");
            sb.AppendLine();
            sb.AppendLine("    {");
            foreach (var name in matches)
            {
                sb.AppendLine($"        \"{name}\",");
            }
            sb.AppendLine("    };");
            if (r < KnownRoles.Length - 1)
            {
                sb.AppendLine();
            }
        }
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string ToMemberName(string permission) => permission.Replace('.', '_');
}
```

- [ ] **Step 2: Build the generator**

Run:

```bash
dotnet build backend/src/CCE.Domain.SourceGenerators/CCE.Domain.SourceGenerators.csproj --nologo --no-restore 2>&1 | tail -8
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`. If a Roslyn analyzer warning fires (RS1xxx), check that you didn't add a banned API; the curated NoWarn covers RS2008+RS1036.

- [ ] **Step 3: Run all generator tests — expect all green**

Run:

```bash
dotnet test backend/tests/CCE.Domain.SourceGenerators.Tests/CCE.Domain.SourceGenerators.Tests.csproj --nologo --logger "console;verbosity=minimal" 2>&1 | tail -8
```

Expected: `Passed:    10` (2 smoke + 4 nested + 4 RolePermissionMap = 10).

- [ ] **Step 4: Run the existing CCE.Domain.Tests to confirm Foundation regression net is intact**

Run:

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --logger "console;verbosity=minimal" 2>&1 | tail -6
```

Expected: `Passed:    25` (16 baseline + 6 Common + 3 PermissionsYamlSchema). If `Permissions.System_Health_Read` regression test fails, **stop** — the generator's flat-schema branch broke.

- [ ] **Step 5: Commit (GREEN)**

```bash
git add backend/src/CCE.Domain.SourceGenerators/PermissionsGenerator.cs
git -c commit.gpgsign=false commit -m "feat(sourcegen): GREEN — extend PermissionsGenerator with nested-groups schema + RolePermissionMap (8 tests pass)"
```

---

## Task 1.5: Replace `permissions.yaml` with the full BRD §4.1.31 matrix

**Files:**
- Modify: `backend/permissions.yaml` (replace entirely)

**Rationale:** With the generator now able to handle nested + roles, populate the YAML with all 41 permissions across 6 roles per spec §5.6. This is the actual delivery of Phase 01 — the source code now exposes `Permissions.User_Read`, `RolePermissionMap.SuperAdmin`, etc.

- [ ] **Step 1: Replace `backend/permissions.yaml` with the full matrix**

`backend/permissions.yaml`:

```yaml
# CCE — Permission matrix
# Single source of truth for all authorization permissions. Generated into
# `CCE.Domain.Permissions` and `CCE.Domain.RolePermissionMap` by:
#   backend/src/CCE.Domain.SourceGenerators/PermissionsGenerator.cs
#
# Schema:
#   groups:
#     <Group>:
#       <SubGroupOrAction>:
#         description: <human-readable description>
#         roles: [<Role1>, <Role2>, ...]
#
# Naming rules:
#   - PascalCase segments only.
#   - Verbs: Read / Create / Update / Delete / Approve / Reject / List / Search / Edit / Manage
#     / Restore / Run / Submit / Publish / View / Assign / Moderate / Follow / Rate / Reply.
#   - Stable: never rename — deprecate old + add new instead.
#
# Known roles (defined in PermissionsGenerator.KnownRoles):
#   SuperAdmin, ContentManager, StateRepresentative, CommunityExpert, RegisteredUser, Anonymous

groups:
  System:
    Health:
      Read:
        description: Read system health probe
        roles: [SuperAdmin]
  User:
    Read:
      description: Read user profiles
      roles: [SuperAdmin, ContentManager]
    Create:
      description: Create user accounts (admin path)
      roles: [SuperAdmin]
    Update:
      description: Update user profile fields (admin path)
      roles: [SuperAdmin]
    Delete:
      description: Soft-delete a user
      roles: [SuperAdmin]
    Restore:
      description: Undelete a previously soft-deleted user
      roles: [SuperAdmin]
  Role:
    Assign:
      description: Assign a role to a user
      roles: [SuperAdmin]
  Resource:
    Center:
      Upload:
        description: Upload a center-managed resource
        roles: [SuperAdmin, ContentManager]
      Update:
        description: Edit a center-managed resource
        roles: [SuperAdmin, ContentManager]
      Delete:
        description: Soft-delete a center resource
        roles: [SuperAdmin, ContentManager]
    Country:
      Approve:
        description: Approve a country resource request
        roles: [SuperAdmin, ContentManager]
      Reject:
        description: Reject a country resource request
        roles: [SuperAdmin, ContentManager]
      Submit:
        description: State rep submits a country resource for approval
        roles: [StateRepresentative]
  News:
    Publish:
      description: Publish news articles
      roles: [SuperAdmin, ContentManager]
    Update:
      description: Edit news article
      roles: [SuperAdmin, ContentManager]
    Delete:
      description: Soft-delete news article
      roles: [SuperAdmin, ContentManager]
  Event:
    Manage:
      description: Create/update/delete events
      roles: [SuperAdmin, ContentManager]
  Page:
    Edit:
      description: Edit static pages (about, terms, privacy)
      roles: [SuperAdmin, ContentManager]
  Country:
    Profile:
      Update:
        description: Edit country profile content
        roles: [SuperAdmin, ContentManager, StateRepresentative]
  Community:
    Post:
      Create:
        description: Create a community post
        roles: [RegisteredUser, CommunityExpert, StateRepresentative, SuperAdmin]
      Reply:
        description: Reply to a community post
        roles: [RegisteredUser, CommunityExpert, StateRepresentative, SuperAdmin]
      Rate:
        description: Rate a community post
        roles: [RegisteredUser, CommunityExpert, StateRepresentative, SuperAdmin]
      Moderate:
        description: Soft-delete or restore a community post (moderation)
        roles: [SuperAdmin, ContentManager]
      Follow:
        description: Follow posts/topics/users
        roles: [RegisteredUser, CommunityExpert, StateRepresentative, SuperAdmin]
    Expert:
      RegisterRequest:
        description: Submit expert registration request
        roles: [RegisteredUser]
      ApproveRequest:
        description: Approve or reject an expert registration request
        roles: [SuperAdmin, ContentManager]
  KnowledgeMap:
    View:
      description: View knowledge maps
      roles: [Anonymous, RegisteredUser, CommunityExpert, StateRepresentative, ContentManager, SuperAdmin]
    Manage:
      description: Create/update/delete knowledge maps
      roles: [SuperAdmin, ContentManager]
  InteractiveCity:
    Run:
      description: Run an Interactive City simulation
      roles: [Anonymous, RegisteredUser, CommunityExpert, StateRepresentative, ContentManager, SuperAdmin]
    SaveScenario:
      description: Save a scenario to user profile
      roles: [RegisteredUser, CommunityExpert, StateRepresentative, ContentManager, SuperAdmin]
  Survey:
    Submit:
      description: Submit a service rating
      roles: [Anonymous, RegisteredUser, CommunityExpert, StateRepresentative, ContentManager, SuperAdmin]
    ReadAll:
      description: Read all survey responses
      roles: [SuperAdmin]
  Notification:
    TemplateManage:
      description: Manage notification templates
      roles: [SuperAdmin]
  Report:
    UserRegistrations:
      description: Generate user-registration report
      roles: [SuperAdmin]
    ExpertList:
      description: Generate community-experts report
      roles: [SuperAdmin]
    SatisfactionSurvey:
      description: Generate satisfaction-survey report
      roles: [SuperAdmin]
    CommunityPosts:
      description: Generate community-posts report
      roles: [SuperAdmin]
    News:
      description: Generate news report
      roles: [SuperAdmin]
    Events:
      description: Generate events report
      roles: [SuperAdmin]
    Resources:
      description: Generate resources report
      roles: [SuperAdmin]
    CountryProfiles:
      description: Generate country profiles report
      roles: [SuperAdmin]
```

- [ ] **Step 2: Build everything**

Run:

```bash
dotnet build backend/CCE.sln --nologo --no-restore 2>&1 | tail -6
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`. If `error CS0103: The name 'Permissions.System_Health_Read' does not exist` (or similar) appears anywhere, the schema-detection branch is broken — **stop** and fix.

- [ ] **Step 3: Verify the generated source contains the expected counts**

The generator's output isn't checked into git, but the symbols are visible at compile time. Use a one-shot compile-time probe via the test harness instead:

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo --logger "console;verbosity=minimal" 2>&1 | tail -6
```

Expected: `Passed:    25`. (Confirms the existing `Permissions.All.Count` test passes — the new yaml exposes 41 permissions, so `Permissions.All.Count` is 41, well above the existing `> 0` assertion.)

- [ ] **Step 4: Add a permanent BRD coverage guard test**

Add to `backend/tests/CCE.Domain.Tests/PermissionsYamlSchemaTests.cs`:

Open the file, locate the closing `}` of the class, and insert before it:

```csharp
    [Fact]
    public void All_BRD_required_permissions_are_present()
    {
        // Sentinel set: one permission per BRD §4.1.31 group. If the YAML drifts and a whole
        // group disappears, this test catches it. (Per-role maps are validated in source-gen tests.)
        var required = new[]
        {
            "System.Health.Read",
            "User.Read",
            "Role.Assign",
            "Resource.Center.Upload",
            "Resource.Country.Submit",
            "News.Publish",
            "Event.Manage",
            "Page.Edit",
            "Country.Profile.Update",
            "Community.Post.Create",
            "Community.Expert.RegisterRequest",
            "KnowledgeMap.View",
            "InteractiveCity.Run",
            "Survey.Submit",
            "Notification.TemplateManage",
            "Report.UserRegistrations",
        };
        Permissions.All.Should().Contain(required);
    }

    [Fact]
    public void Permissions_All_count_matches_BRD_matrix()
    {
        // Spec §5.6 enumerates exactly 41 permissions. If you add or remove one, update this number
        // and update docs/superpowers/specs/2026-04-27-data-domain-design.md §5.6 in the same PR.
        Permissions.All.Count.Should().Be(41);
    }

    [Fact]
    public void RolePermissionMap_emits_all_six_known_roles()
    {
        var roles = typeof(CCE.Domain.RolePermissionMap)
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(p => p.Name)
            .ToHashSet();

        roles.Should().BeEquivalentTo(new[]
        {
            "SuperAdmin", "ContentManager", "StateRepresentative",
            "CommunityExpert", "RegisteredUser", "Anonymous",
        });
    }

    [Fact]
    public void SuperAdmin_role_has_every_permission_assigned_to_it_in_YAML()
    {
        // SuperAdmin appears on most permissions in the matrix — at minimum it owns all reports
        // and all System/User/Role permissions. Sentinel covers high-leverage entries.
        var superAdmin = CCE.Domain.RolePermissionMap.SuperAdmin;
        superAdmin.Should().Contain(new[]
        {
            "System.Health.Read",
            "User.Read",
            "Role.Assign",
            "Report.UserRegistrations",
            "Report.News",
        });
    }
```

- [ ] **Step 5: Build + run all backend tests**

Run:

```bash
dotnet build backend/CCE.sln --nologo --no-restore 2>&1 | tail -6
```

Expected: 0 errors, 0 warnings.

Run:

```bash
dotnet test backend/CCE.sln --nologo --no-build --logger "console;verbosity=minimal" 2>&1 | grep -E "(Passed!|Failed!)"
```

Expected (5 result lines):

```
Passed!  - Failed:     0, Passed:    29, Skipped:     0, Total:    29 ... CCE.Domain.Tests.dll
Passed!  - Failed:     0, Passed:    12, Skipped:     0, Total:    12 ... CCE.Application.Tests.dll
Passed!  - Failed:     0, Passed:    28, Skipped:     0, Total:    28 ... CCE.Api.IntegrationTests.dll
Passed!  - Failed:     0, Passed:     6, Skipped:     0, Total:     6 ... CCE.Infrastructure.Tests.dll
Passed!  - Failed:     0, Passed:    10, Skipped:     0, Total:    10 ... CCE.Domain.SourceGenerators.Tests.dll
```

Backend total = 29 + 12 + 28 + 6 + 10 = 85.

If `Permissions_All_count_matches_BRD_matrix` fails with a count other than 41, **stop** — count and reconcile against spec §5.6 (don't change the assertion to make it pass).

- [ ] **Step 6: Commit**

```bash
git add backend/permissions.yaml backend/tests/CCE.Domain.Tests/PermissionsYamlSchemaTests.cs
git -c commit.gpgsign=false commit -m "feat(permissions): expand permissions.yaml to full BRD §4.1.31 matrix (41 perms × 6 roles) + 4 BRD-coverage tests"
```

---

## Task 1.6: Update progress tracker + close Phase 01

**Files:**
- Modify: `docs/subprojects/02-data-domain-progress.md`

**Rationale:** Mark Phase 01 done, bump Domain test count (25 → 29) + add Source generator (0 → 10) row + cumulative (71 → 85).

- [ ] **Step 1: Update phase-status row**

Open `docs/subprojects/02-data-domain-progress.md`. Replace:

```markdown
| 01 | Permissions YAML + source-gen | ⏳ Pending |
```

with:

```markdown
| 01 | Permissions YAML + source-gen | ✅ Done |
```

- [ ] **Step 2: Update test totals**

Replace the existing test totals table with:

```markdown
| Layer | At start | Current | Target |
|---|---|---|---|
| Domain | 16 | 29 | ~136 |
| Application | 12 | 12 | ~72 |
| Infrastructure | 6 | 6 | ~46 |
| Architecture | 0 | 0 | ~15 |
| Source generator | 0 | 10 | ~20 |
| Api Integration | 28 | 28 | ~38 |
| **Cumulative** | **62** (backend) | **85** | **~327** (backend) |
```

- [ ] **Step 3: Commit**

```bash
git add docs/subprojects/02-data-domain-progress.md
git -c commit.gpgsign=false commit -m "docs(sub-2): mark Phase 01 done; test totals 29 Domain / 10 SourceGen / 85 backend"
```

---

## Phase 01 — completion checklist

- [ ] `CCE.Domain.SourceGenerators.Tests` project exists in solution; 10 tests pass.
- [ ] `PermissionsGenerator` parses both flat (`permissions:`) and nested (`groups:`) schemas.
- [ ] Generator emits `RolePermissionMap.<Role>` for all 6 known roles.
- [ ] `backend/permissions.yaml` contains 41 permissions across 6 roles.
- [ ] `Permissions.All.Count == 41` (asserted by `Permissions_All_count_matches_BRD_matrix`).
- [ ] All Foundation Domain tests still pass (regression net intact).
- [ ] `dotnet build backend/CCE.sln` 0 errors / 0 warnings.
- [ ] `dotnet test backend/CCE.sln` reports 85 backend passing.
- [ ] `git status` clean.
- [ ] 6 new commits with the messages shown above.

**If all boxes ticked, Phase 01 is complete. Proceed to Phase 02 (Identity bounded context).**
