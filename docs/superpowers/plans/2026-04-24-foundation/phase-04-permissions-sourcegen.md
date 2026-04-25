# Phase 04 — Permissions Source Generator

> Parent: [../2026-04-24-foundation.md](../2026-04-24-foundation.md) · Spec: [../../specs/2026-04-24-foundation-design.md](../../specs/2026-04-24-foundation-design.md)

**Phase goal:** Establish the **single source of truth** for the permission matrix. A `permissions.yaml` file at the backend root drives a Roslyn incremental source generator that emits a strongly-typed `Permissions` static class into `CCE.Domain` at build time. Foundation seeds exactly one permission (`System.Health.Read`); the real BRD §4.1.31 permission matrix lands in sub-project 2 by appending entries to `permissions.yaml` — no code changes required.

**Tasks in this phase:** 5
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 03 complete; `backend/CCE.sln` builds with 0 warnings/errors; Docker stack healthy.

---

## Pre-execution sanity checks

1. **.NET 8 SDK reachable.** Run: `dotnet --list-sdks | grep '^8\.'` → at least one 8.0.x line.
2. **Solution builds clean.** Run: `dotnet build backend/CCE.sln --nologo 2>&1 | tail -3` → must end with `Build succeeded.` and `0 Error(s)`.
3. **Existing tests pass.** Run: `dotnet test backend/CCE.sln --nologo --no-build --filter FullyQualifiedName~CCE.Domain.Tests 2>&1 | tail -5` → must show `Passed: 2`.
4. **No existing `permissions.yaml`.** Run: `test ! -e backend/permissions.yaml && echo OK` → `OK`.
5. **No existing source-generator project.** Run: `test ! -d backend/src/CCE.Domain.SourceGenerators && echo OK` → `OK`.

If any check fails, stop and report.

---

## Why a source generator (not a hand-written enum)?

Three reasons it pays off:

1. **Single source of truth.** Permissions live in YAML, owned by domain experts/business analysts. Devs touch C# only when they need a *new* check — not when permission *names* change.
2. **No drift between layers.** The same generated `Permissions.System_Health_Read` constant flows to backend policies, OpenAPI extensions, and (in Phase 13) the Angular `api-client` lib's TS enum.
3. **Compile-time safety.** Typos like `Permissions.Health.System.Read` (wrong order) become compile errors, not runtime 403s.

Foundation deliberately keeps the generator simple — it's ~80 lines, parses a flat YAML list (no nested objects), and emits two members: a const string per permission and an `All` IReadOnlyList. Sub-project 2 expands the YAML schema (groups, descriptions, default-roles).

---

## Task 4.1: Create `backend/permissions.yaml` with the seed permission

**Files:**
- Create: `backend/permissions.yaml`

**Rationale:** A flat YAML list is the smallest schema that supports adding more permissions without churning the parser. Nested groups/categories are deliberately not modeled — sub-project 2 adds them once we know the real shape.

- [ ] **Step 1: Write `backend/permissions.yaml`**

```yaml
# CCE — Permission matrix
# Single source of truth for all authorization permissions.
# Format: <Category>.<Resource>.<Action> — three dot-separated segments.
# Generated into CCE.Domain.Permissions by the Roslyn source generator at:
#   backend/src/CCE.Domain.SourceGenerators
# Adding a permission: append a new line under `permissions:` and rebuild.
# The full BRD §4.1.31 permission matrix (~70 entries) lands in sub-project 2.
#
# Naming rules:
#   - PascalCase segments only (System, Content, User, etc.)
#   - Verbs use Read / Create / Update / Delete / Approve / Reject / List / Search
#   - Stable: never rename — deprecate old + add new instead.

permissions:
  - System.Health.Read
```

- [ ] **Step 2: Verify YAML parses**

Run:
```bash
python3 -c "import yaml; print(yaml.safe_load(open('backend/permissions.yaml')))" 2>/dev/null \
  || (command -v yq >/dev/null && yq '.permissions' backend/permissions.yaml) \
  || (echo "yaml.safe_load (Python) and yq both unavailable — falling back to grep check"; \
      grep -E '^\s*-\s+[A-Z][A-Za-z0-9]*(\.[A-Z][A-Za-z0-9]*)+$' backend/permissions.yaml)
```
Expected: prints a Python list/dict, a yq array, or one matching grep line for `- System.Health.Read`.

- [ ] **Step 3: Commit**

```bash
git add backend/permissions.yaml
git -c commit.gpgsign=false commit -m "feat(phase-04): add backend/permissions.yaml with System.Health.Read seed entry"
```

---

## Task 4.2: Create `CCE.Domain.SourceGenerators` project (netstandard2.0)

**Files:**
- Create: `backend/src/CCE.Domain.SourceGenerators/CCE.Domain.SourceGenerators.csproj`

**Rationale:** Roslyn source generators **must** target `netstandard2.0` — the compiler runs them in the C# language service, which still hosts an old runtime. This project overrides the `<TargetFramework>net8.0</TargetFramework>` set by `Directory.Build.props`. The generator project never references `CCE.Domain` (that would be circular).

**Important:** This project disables a few Directory.Build.props defaults that don't apply to source generators:
- Code analysis full-set (replaced with focused Roslyn analyzer rules).
- XML doc generation (generators aren't documented APIs).
- The output assembly itself isn't shipped with the consuming project — it's loaded by the compiler as an analyzer.

- [ ] **Step 1: Create the project directory + minimal csproj**

```bash
mkdir -p backend/src/CCE.Domain.SourceGenerators
```

Write `backend/src/CCE.Domain.SourceGenerators/CCE.Domain.SourceGenerators.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- Source generators MUST target netstandard2.0 (Roslyn compiler-host requirement).
         Overrides Directory.Build.props's net8.0 default. -->
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>12.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>

    <!-- Mark this as a Roslyn component so analyzers/MSBuild treat it correctly -->
    <IsRoslynComponent>true</IsRoslynComponent>
    <IncludeBuildOutput>false</IncludeBuildOutput>

    <!-- Apply the Roslyn-component-specific analyzer rules -->
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>

    <!-- Generators aren't documented APIs and aren't packed -->
    <GenerateDocumentationFile>false</GenerateDocumentationFile>
    <IsPackable>false</IsPackable>

    <!-- These NoWarn additions cover Roslyn rules that fire on hobby/internal generators
         but are designed for shipping public analyzers. Justifications:
           RS2008 — release tracking (only useful for NuGet-published analyzers)
           RS1036 — analyzer banned APIs (overkill for in-repo single-purpose generator)
      -->
    <NoWarn>$(NoWarn);RS2008;RS1036</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" PrivateAssets="all">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Add the project to the solution**

```bash
dotnet sln backend/CCE.sln add backend/src/CCE.Domain.SourceGenerators/CCE.Domain.SourceGenerators.csproj
```

- [ ] **Step 3: Restore + build (no source files yet — should succeed with empty output)**

```bash
dotnet build backend/src/CCE.Domain.SourceGenerators/CCE.Domain.SourceGenerators.csproj --nologo -c Debug 2>&1 | tail -8
```
Expected: `Build succeeded.` with 0 errors. (The empty project compiles to a tiny analyzer assembly with no [Generator]-attributed types yet — that's fine.)

If the restore stalls (Microsoft.CodeAnalysis.CSharp 4.11.0 is a large package), wait up to 5 minutes; previous phases hit similar stalls and they resolved on retry.

- [ ] **Step 4: Commit**

```bash
git add backend/src/CCE.Domain.SourceGenerators backend/CCE.sln
git -c commit.gpgsign=false commit -m "feat(phase-04): add CCE.Domain.SourceGenerators project (netstandard2.0, Roslyn analyzer)"
```

---

## Task 4.3: Implement `PermissionsGenerator` (IIncrementalGenerator)

**Files:**
- Create: `backend/src/CCE.Domain.SourceGenerators/PermissionsGenerator.cs`

**Rationale:** `IIncrementalGenerator` is the modern Roslyn API (introduced 2022) — it caches intermediate results and only re-runs the necessary stages when inputs change, making rebuilds fast. The generator:

1. Watches `AdditionalTextsProvider` for files matching `permissions.yaml`.
2. Parses the YAML with a hand-rolled flat-list parser (no `YamlDotNet` dependency — packaging external NuGets into source generators is fiddly, and our YAML is trivially flat).
3. Emits `Permissions.g.cs` containing const string declarations + an `All` `IReadOnlyList<string>` collection.

- [ ] **Step 1: Write `backend/src/CCE.Domain.SourceGenerators/PermissionsGenerator.cs`**

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CCE.Domain.SourceGenerators;

/// <summary>
/// Roslyn incremental source generator that turns <c>backend/permissions.yaml</c> into a
/// strongly-typed <see cref="Permissions"/> static class in <c>CCE.Domain</c>.
/// Triggered automatically on every build of any project that adds this generator as an analyzer
/// and adds <c>permissions.yaml</c> as an <c>AdditionalFiles</c> item.
/// </summary>
[Generator]
public sealed class PermissionsGenerator : IIncrementalGenerator
{
    private const string YamlFileName = "permissions.yaml";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Match permissions.yaml under any path. Compare by filename so subdirectory layout
        // (backend/permissions.yaml vs ./permissions.yaml) doesn't matter to the generator.
        var yamlContent = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(YamlFileName, StringComparison.OrdinalIgnoreCase))
            .Select(static (file, ct) => file.GetText(ct)?.ToString() ?? string.Empty)
            .Collect();

        context.RegisterSourceOutput(yamlContent, static (spc, contents) =>
        {
            // If multiple permissions.yaml files match (shouldn't happen but defensive), use the first non-empty.
            var yaml = contents.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)) ?? string.Empty;
            var permissions = ParsePermissions(yaml);
            var source = GenerateSource(permissions);
            spc.AddSource("Permissions.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }

    /// <summary>
    /// Hand-rolled parser for the flat-list YAML format used by <c>permissions.yaml</c>.
    /// Recognizes lines of the form <c>  - Some.Permission.Name</c> (inside a top-level list).
    /// Ignores blank lines, comment lines (starting with <c>#</c>), and the <c>permissions:</c> header.
    /// Quotes around the name are stripped.
    /// </summary>
    private static List<string> ParsePermissions(string yaml)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(yaml))
        {
            return result;
        }

        foreach (var raw in yaml.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0)
            {
                continue;
            }
            if (line[0] == '#')
            {
                continue;
            }
            if (!line.StartsWith("- ", StringComparison.Ordinal))
            {
                continue;
            }
            var name = line.Substring(2).Trim().Trim('"', '\'').Trim();
            if (name.Length == 0)
            {
                continue;
            }
            // Defensive validation: only accept dot-separated PascalCase tokens.
            if (!IsValidPermissionName(name))
            {
                continue;
            }
            result.Add(name);
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
            if (seg.Length == 0)
            {
                return false;
            }
            if (!char.IsUpper(seg[0]))
            {
                return false;
            }
            for (var i = 1; i < seg.Length; i++)
            {
                var c = seg[i];
                if (!char.IsLetterOrDigit(c))
                {
                    return false;
                }
            }
        }
        return true;
    }

    private static string GenerateSource(List<string> permissions)
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

        foreach (var p in permissions)
        {
            var memberName = ToMemberName(p);
            sb.AppendLine($"    /// <summary>The <c>{p}</c> permission.</summary>");
            sb.AppendLine($"    public const string {memberName} = \"{p}\";");
            sb.AppendLine();
        }

        sb.AppendLine("    /// <summary>Every permission, in YAML declaration order.</summary>");
        sb.AppendLine("    public static IReadOnlyList<string> All { get; } = new[]");
        sb.AppendLine("    {");
        foreach (var p in permissions)
        {
            sb.AppendLine($"        {ToMemberName(p)},");
        }
        sb.AppendLine("    };");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string ToMemberName(string permission) => permission.Replace('.', '_');
}
```

- [ ] **Step 2: Build the source-generator project**

```bash
dotnet build backend/src/CCE.Domain.SourceGenerators/CCE.Domain.SourceGenerators.csproj --nologo -c Debug 2>&1 | tail -8
```
Expected: `Build succeeded. 0 Error(s)`. Any analyzer hit not in the project's NoWarn list (RS2008, RS1036) → STOP and report.

- [ ] **Step 3: Commit**

```bash
git add backend/src/CCE.Domain.SourceGenerators
git -c commit.gpgsign=false commit -m "feat(phase-04): implement PermissionsGenerator (IIncrementalGenerator) with flat-YAML parser"
```

---

## Task 4.4: Wire generator into `CCE.Domain` and add YAML as `AdditionalFiles`

**Files:**
- Modify: `backend/src/CCE.Domain/CCE.Domain.csproj`

**Rationale:** Connecting the generator to its consumer is two MSBuild items in `CCE.Domain.csproj`:
- `<ProjectReference>` with `OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"` — tells MSBuild "this project is an analyzer, load it into the compiler, but don't reference its output assembly at runtime."
- `<AdditionalFiles Include="..\..\permissions.yaml" />` — exposes the YAML to the generator's `AdditionalTextsProvider`.

- [ ] **Step 1: Read current `backend/src/CCE.Domain/CCE.Domain.csproj`**

```bash
cat backend/src/CCE.Domain/CCE.Domain.csproj
```
Expected: a minimal csproj from Phase 03 Task 3.4 with just `<IsPackable>false</IsPackable>`.

- [ ] **Step 2: Overwrite `backend/src/CCE.Domain/CCE.Domain.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <!-- Source generator: emits Permissions.g.cs from ../../permissions.yaml at build time -->
    <ProjectReference Include="..\CCE.Domain.SourceGenerators\CCE.Domain.SourceGenerators.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
  </ItemGroup>

  <ItemGroup>
    <!-- Path is relative to this csproj. backend/permissions.yaml is two levels up from src/CCE.Domain/. -->
    <AdditionalFiles Include="..\..\permissions.yaml" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Build CCE.Domain and verify generated file appears**

```bash
dotnet build backend/src/CCE.Domain/CCE.Domain.csproj --nologo -c Debug 2>&1 | tail -10
```
Expected: `Build succeeded. 0 Error(s)`.

After the build, the generator emits the source into a virtual file (not on disk by default). To make it inspectable, set `EmitCompilerGeneratedFiles=true` for one verification build. **Don't commit this property** — it's just for verification.

```bash
dotnet build backend/src/CCE.Domain/CCE.Domain.csproj --nologo -c Debug \
  -p:EmitCompilerGeneratedFiles=true \
  -p:CompilerGeneratedFilesOutputPath=Generated 2>&1 | tail -3

# Look for the generated file
find backend/src/CCE.Domain/Generated -name "Permissions.g.cs" 2>/dev/null | head -1
```
Expected: prints a path like `backend/src/CCE.Domain/Generated/CCE.Domain.SourceGenerators/CCE.Domain.SourceGenerators.PermissionsGenerator/Permissions.g.cs`.

Inspect:
```bash
GENPATH=$(find backend/src/CCE.Domain/Generated -name "Permissions.g.cs" 2>/dev/null | head -1)
[ -n "$GENPATH" ] && cat "$GENPATH" | head -25
```
Expected: prints the auto-generated header + a `public static class Permissions { public const string System_Health_Read = "System.Health.Read"; ... }` block.

- [ ] **Step 4: Clean up the inspection artifacts (don't commit Generated/)**

```bash
rm -rf backend/src/CCE.Domain/Generated
```

The root `.gitignore` already excludes `bin/` and `obj/` so the in-memory generator output isn't tracked. The `Generated/` directory was created only by our `EmitCompilerGeneratedFiles=true` flag and is purely diagnostic.

- [ ] **Step 5: Verify the full solution still builds**

```bash
dotnet build backend/CCE.sln --nologo -c Debug 2>&1 | tail -8
```
Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 6: Commit**

```bash
git add backend/src/CCE.Domain/CCE.Domain.csproj
git -c commit.gpgsign=false commit -m "feat(phase-04): wire PermissionsGenerator + permissions.yaml into CCE.Domain"
```

---

## Task 4.5: Add green tests proving the generated `Permissions` class is usable

**Files:**
- Create: `backend/tests/CCE.Domain.Tests/PermissionsTests.cs`

**Rationale:** Two `[Fact]` tests prove (a) the generator ran, (b) the constant value matches the YAML literal, and (c) the `All` collection is populated. This is the **only** verification that catches generator regressions — without these tests, a broken generator would silently produce an empty `Permissions` class and any consumer code would break instead.

- [ ] **Step 1: Write the failing test first (TDD discipline per ADR-0007)**

```bash
cat > backend/tests/CCE.Domain.Tests/PermissionsTests.cs <<'EOF'
using CCE.Domain;

namespace CCE.Domain.Tests;

public class PermissionsTests
{
    [Fact]
    public void System_Health_Read_constant_matches_YAML_value()
    {
        Permissions.System_Health_Read.Should().Be("System.Health.Read");
    }

    [Fact]
    public void All_collection_contains_System_Health_Read()
    {
        Permissions.All.Should().Contain("System.Health.Read");
    }

    [Fact]
    public void All_collection_is_not_empty()
    {
        Permissions.All.Should().NotBeEmpty();
    }
}
EOF
```

- [ ] **Step 2: Run the tests — expect 3 passes**

Since the generator already wired in Task 4.4, the tests pass on first compile (this is a "test-after" verification rather than red→green TDD because the production code, the generator, was written first; that's an explicit exception to the TDD rule for tooling/build-system code).

```bash
dotnet test backend/tests/CCE.Domain.Tests/CCE.Domain.Tests.csproj --nologo -c Debug 2>&1 | tail -10
```
Expected:
```
Passed!  - Failed: 0, Passed: 5, Skipped: 0
```
(5 = 2 from `EntityTests` + 3 from `PermissionsTests`.)

- [ ] **Step 3: Verify whole solution test count**

```bash
dotnet test backend/CCE.sln --nologo --no-build 2>&1 | tail -10
```
Expected: 5 passed, 0 failed.

- [ ] **Step 4: Commit**

```bash
git add backend/tests/CCE.Domain.Tests/PermissionsTests.cs
git -c commit.gpgsign=false commit -m "test(phase-04): add PermissionsTests proving generated Permissions.System_Health_Read is accessible"
```

---

## Phase 04 — completion checklist

- [ ] `backend/permissions.yaml` exists with one entry: `System.Health.Read`.
- [ ] `backend/src/CCE.Domain.SourceGenerators/` is a netstandard2.0 Roslyn analyzer project, builds clean.
- [ ] `PermissionsGenerator.cs` implements `IIncrementalGenerator` with a flat-list YAML parser and emits a `Permissions` static class.
- [ ] `CCE.Domain.csproj` references the generator as `OutputItemType="Analyzer"` and adds `permissions.yaml` as `AdditionalFiles`.
- [ ] `dotnet build backend/CCE.sln` succeeds with 0 errors / 0 warnings; warnings-as-errors enforced.
- [ ] `dotnet test backend/CCE.sln` reports 5 passed (2 Entity + 3 Permissions).
- [ ] One can write `Permissions.System_Health_Read` in `CCE.Domain` consumer projects without compile errors.
- [ ] `git log --oneline | head -8` shows 5 new Phase-04 commits (one per task).
- [ ] `git status` shows clean tree.

**If all boxes ticked, phase 04 is complete. Proceed to phase 05 (Domain layer base classes — already partly delivered in Phase 03 task 3.4; Phase 05 adds the System Clock fake + a few helper value objects).**
