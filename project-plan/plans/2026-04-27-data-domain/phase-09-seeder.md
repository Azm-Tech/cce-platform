# Phase 09 — Seeders (Roles + Reference data + Demo)

> Parent: [`../2026-04-27-data-domain.md`](../2026-04-27-data-domain.md) · Spec §5.7

**Phase goal:** Idempotent seeders that populate the dev/test/prod DB with: (1) the 5 named roles (Anonymous is implicit) wired to `RolePermissionMap` permissions, (2) a starter set of reference data (countries, city technologies, knowledge-map nodes, notification templates, community topics, resource categories, static pages, homepage sections), and (3) optional demo data behind a `--demo` flag.

**Tasks in this phase:** 8
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 08 closed (`08ced9c` HEAD); migration applied; 347 backend tests passing.

**Determinism:** Every seeded entity uses a deterministic GUID computed from its natural key (e.g., country ISO-alpha-3 → md5 → first 16 bytes → Guid). Re-running the seeder MUST find the row already present and skip it; no double-inserts.

---

## Task 9.1: New `CCE.Seeder` library + `SeedingExtensions` DI

**Files:**
- Modify: `backend/CCE.sln` (add new project)
- Create: `backend/src/CCE.Seeder/CCE.Seeder.csproj`
- Create: `backend/src/CCE.Seeder/ISeeder.cs`
- Create: `backend/src/CCE.Seeder/SeedRunner.cs`
- Create: `backend/src/CCE.Seeder/DeterministicGuid.cs`

**Rationale:** A library project (not a CLI) means the API host can run seeders at startup AND a future console runner can call into the same code. Avoids duplicating logic.

- [ ] **Step 1: csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" />
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\CCE.Application\CCE.Application.csproj" />
    <ProjectReference Include="..\CCE.Domain\CCE.Domain.csproj" />
    <ProjectReference Include="..\CCE.Infrastructure\CCE.Infrastructure.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: `ISeeder.cs`**

```csharp
namespace CCE.Seeder;

public interface ISeeder
{
    /// <summary>Lower runs first. Roles → reference → demo.</summary>
    int Order { get; }

    /// <summary>Idempotent. Must be safe to run repeatedly.</summary>
    Task SeedAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 3: `SeedRunner.cs`**

```csharp
using Microsoft.Extensions.Logging;

namespace CCE.Seeder;

/// <summary>
/// Orchestrates registered <see cref="ISeeder"/>s in <c>Order</c> ascending. Logs each step.
/// Failures bubble up — caller decides whether to abort startup.
/// </summary>
public sealed class SeedRunner
{
    private readonly IEnumerable<ISeeder> _seeders;
    private readonly ILogger<SeedRunner> _logger;

    public SeedRunner(IEnumerable<ISeeder> seeders, ILogger<SeedRunner> logger)
    {
        _seeders = seeders;
        _logger = logger;
    }

    public async Task RunAllAsync(bool includeDemo = false, CancellationToken ct = default)
    {
        var ordered = _seeders
            .Where(s => includeDemo || s.GetType().Name != "DemoDataSeeder")
            .OrderBy(s => s.Order)
            .ToList();

        _logger.LogInformation("Running {Count} seeders (demo={Demo}).", ordered.Count, includeDemo);

        foreach (var seeder in ordered)
        {
            var name = seeder.GetType().Name;
            _logger.LogInformation("→ {Seeder}", name);
            await seeder.SeedAsync(ct).ConfigureAwait(false);
        }

        _logger.LogInformation("Seeders complete.");
    }
}
```

- [ ] **Step 4: `DeterministicGuid.cs`**

```csharp
using System.Security.Cryptography;
using System.Text;

namespace CCE.Seeder;

/// <summary>
/// Deterministic Guid derived from a string. Used by seeders so re-runs
/// match existing rows by Id rather than creating duplicates.
/// </summary>
public static class DeterministicGuid
{
    /// <summary>SHA-1 → first 16 bytes → Guid. Stable across processes.</summary>
    public static System.Guid From(string seed)
    {
        if (string.IsNullOrEmpty(seed)) throw new System.ArgumentException("seed required", nameof(seed));
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(seed));
        var bytes = new byte[16];
        System.Array.Copy(hash, bytes, 16);
        return new System.Guid(bytes);
    }
}
```

- [ ] **Step 5: Add to solution + restore + build**

```bash
dotnet sln backend/CCE.sln add backend/src/CCE.Seeder/CCE.Seeder.csproj
dotnet restore backend/src/CCE.Seeder/CCE.Seeder.csproj --source /tmp/local-nuget --source ~/.nuget/packages 2>&1 | tail -3
dotnet build backend/src/CCE.Seeder/CCE.Seeder.csproj --nologo --no-restore 2>&1 | tail -5
```

Expected: 0 errors.

- [ ] **Step 6: Commit**

```bash
git add backend/CCE.sln backend/src/CCE.Seeder/
git -c commit.gpgsign=false commit -m "feat(seeder): scaffold CCE.Seeder library with ISeeder + SeedRunner + DeterministicGuid"
```

---

## Task 9.2: `RolesAndPermissionsSeeder`

**Files:**
- Create: `backend/src/CCE.Seeder/Seeders/RolesAndPermissionsSeeder.cs`
- Create: `backend/tests/CCE.Infrastructure.Tests/Seeder/RolesAndPermissionsSeederTests.cs`

**Rationale:** Creates 5 ASP.NET Identity roles (SuperAdmin, ContentManager, StateRepresentative, CommunityExpert, RegisteredUser — Anonymous is implicit, not stored) and creates `IdentityRoleClaim` rows mapping each role to its permission strings from `RolePermissionMap`.

- [ ] **Step 1: Seeder**

```csharp
using CCE.Domain;
using CCE.Domain.Identity;
using CCE.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CCE.Seeder.Seeders;

public sealed class RolesAndPermissionsSeeder : ISeeder
{
    private static readonly string[] SeededRoleNames =
    {
        "SuperAdmin", "ContentManager", "StateRepresentative",
        "CommunityExpert", "RegisteredUser",
    };

    private readonly CceDbContext _ctx;
    private readonly ILogger<RolesAndPermissionsSeeder> _logger;

    public RolesAndPermissionsSeeder(CceDbContext ctx, ILogger<RolesAndPermissionsSeeder> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }

    public int Order => 10;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var roleName in SeededRoleNames)
        {
            var roleId = DeterministicGuid.From($"role:{roleName}");
            var existing = await _ctx.Set<Role>().FindAsync(new object[] { roleId }, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                _ctx.Set<Role>().Add(new Role(roleName)
                {
                    Id = roleId,
                    NormalizedName = roleName.ToUpperInvariant(),
                });
                _logger.LogInformation("Seeded role {Role}", roleName);
            }

            var permissions = GetPermissionsForRole(roleName);
            foreach (var permission in permissions)
            {
                var claimExists = await _ctx.Set<IdentityRoleClaim<System.Guid>>()
                    .AnyAsync(c => c.RoleId == roleId
                                   && c.ClaimType == "permission"
                                   && c.ClaimValue == permission, cancellationToken)
                    .ConfigureAwait(false);
                if (!claimExists)
                {
                    _ctx.Set<IdentityRoleClaim<System.Guid>>().Add(new IdentityRoleClaim<System.Guid>
                    {
                        RoleId = roleId,
                        ClaimType = "permission",
                        ClaimValue = permission,
                    });
                }
            }
        }
        await _ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static IReadOnlyList<string> GetPermissionsForRole(string roleName) => roleName switch
    {
        "SuperAdmin" => RolePermissionMap.SuperAdmin,
        "ContentManager" => RolePermissionMap.ContentManager,
        "StateRepresentative" => RolePermissionMap.StateRepresentative,
        "CommunityExpert" => RolePermissionMap.CommunityExpert,
        "RegisteredUser" => RolePermissionMap.RegisteredUser,
        _ => System.Array.Empty<string>(),
    };
}
```

- [ ] **Step 2: Tests** — uses InMemory DB

```csharp
using CCE.Domain.Identity;
using CCE.Infrastructure.Persistence;
using CCE.Seeder;
using CCE.Seeder.Seeders;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CCE.Infrastructure.Tests.Seeder;

public class RolesAndPermissionsSeederTests
{
    private static CceDbContext NewContext() =>
        new(new DbContextOptionsBuilder<CceDbContext>()
            .UseInMemoryDatabase(System.Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task First_run_creates_5_roles_with_permissions()
    {
        var ctx = NewContext();
        var seeder = new RolesAndPermissionsSeeder(ctx, NullLogger<RolesAndPermissionsSeeder>.Instance);

        await seeder.SeedAsync();

        var roles = await ctx.Set<Role>().ToListAsync();
        roles.Should().HaveCount(5);
        roles.Select(r => r.Name).Should().Contain(new[] { "SuperAdmin", "ContentManager",
            "StateRepresentative", "CommunityExpert", "RegisteredUser" });

        var claims = await ctx.Set<IdentityRoleClaim<System.Guid>>().ToListAsync();
        claims.Should().NotBeEmpty();
        claims.Where(c => c.ClaimType == "permission").Should().NotBeEmpty();
    }

    [Fact]
    public async Task Second_run_is_idempotent()
    {
        var ctx = NewContext();
        var seeder = new RolesAndPermissionsSeeder(ctx, NullLogger<RolesAndPermissionsSeeder>.Instance);

        await seeder.SeedAsync();
        var afterFirstRoles = await ctx.Set<Role>().CountAsync();
        var afterFirstClaims = await ctx.Set<IdentityRoleClaim<System.Guid>>().CountAsync();

        await seeder.SeedAsync();
        var afterSecondRoles = await ctx.Set<Role>().CountAsync();
        var afterSecondClaims = await ctx.Set<IdentityRoleClaim<System.Guid>>().CountAsync();

        afterSecondRoles.Should().Be(afterFirstRoles);
        afterSecondClaims.Should().Be(afterFirstClaims);
    }

    [Fact]
    public async Task SuperAdmin_has_System_Health_Read_claim()
    {
        var ctx = NewContext();
        var seeder = new RolesAndPermissionsSeeder(ctx, NullLogger<RolesAndPermissionsSeeder>.Instance);
        await seeder.SeedAsync();

        var roleId = DeterministicGuid.From("role:SuperAdmin");
        var hasClaim = await ctx.Set<IdentityRoleClaim<System.Guid>>()
            .AnyAsync(c => c.RoleId == roleId
                           && c.ClaimType == "permission"
                           && c.ClaimValue == "System.Health.Read");
        hasClaim.Should().BeTrue();
    }
}
```

- [ ] **Step 3: Build + reference Seeder from Tests + run + commit**

```bash
# Add Seeder project reference to the test project (if missing)
grep -q "CCE.Seeder" backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj || \
  sed -i '' 's|<ProjectReference Include="..\\..\\src\\CCE.Infrastructure\\CCE.Infrastructure.csproj" />|<ProjectReference Include="..\\..\\src\\CCE.Infrastructure\\CCE.Infrastructure.csproj" />\n    <ProjectReference Include="..\\..\\src\\CCE.Seeder\\CCE.Seeder.csproj" />|' \
  backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj

dotnet build backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj --nologo --no-restore 2>&1 | tail -5
dotnet test backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~RolesAndPermissionsSeederTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed: 3`.

```bash
git add backend/src/CCE.Seeder/Seeders/RolesAndPermissionsSeeder.cs backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj backend/tests/CCE.Infrastructure.Tests/Seeder/RolesAndPermissionsSeederTests.cs
git -c commit.gpgsign=false commit -m "feat(seeder): RolesAndPermissionsSeeder (5 roles + permission claims, idempotent) (3 TDD tests)"
```

---

## Task 9.3: `ReferenceDataSeeder` part 1 — Countries + ResourceCategories + Topics

**Files:**
- Create: `backend/src/CCE.Seeder/Seeders/ReferenceDataSeeder.cs`
- Create: `backend/tests/CCE.Infrastructure.Tests/Seeder/ReferenceDataSeederTests.cs`

The seeder is split into `Seed*` private helpers; this task ships the entity skeleton + the first three sections. Tasks 9.4 + 9.5 add more sections.

- [ ] **Step 1: Seeder**

```csharp
using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.Domain.Content;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CCE.Seeder.Seeders;

/// <summary>
/// Reference-data seeder. Populates lookup tables (countries, categories, topics, technologies,
/// templates, knowledge maps, pages, homepage sections) with values that should exist in every
/// environment. Idempotent.
/// </summary>
public sealed class ReferenceDataSeeder : ISeeder
{
    private readonly CceDbContext _ctx;
    private readonly ISystemClock _clock;
    private readonly ILogger<ReferenceDataSeeder> _logger;

    public ReferenceDataSeeder(CceDbContext ctx, ISystemClock clock, ILogger<ReferenceDataSeeder> logger)
    {
        _ctx = ctx;
        _clock = clock;
        _logger = logger;
    }

    public int Order => 20;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedCountriesAsync(cancellationToken).ConfigureAwait(false);
        await SeedResourceCategoriesAsync(cancellationToken).ConfigureAwait(false);
        await SeedTopicsAsync(cancellationToken).ConfigureAwait(false);
        await _ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static readonly (string Iso3, string Iso2, string NameAr, string NameEn,
        string RegionAr, string RegionEn)[] InitialCountries =
    {
        ("SAU", "SA", "السعودية", "Saudi Arabia", "آسيا", "Asia"),
        ("ARE", "AE", "الإمارات", "United Arab Emirates", "آسيا", "Asia"),
        ("KWT", "KW", "الكويت", "Kuwait", "آسيا", "Asia"),
        ("QAT", "QA", "قطر", "Qatar", "آسيا", "Asia"),
        ("BHR", "BH", "البحرين", "Bahrain", "آسيا", "Asia"),
        ("OMN", "OM", "عُمان", "Oman", "آسيا", "Asia"),
        ("EGY", "EG", "مصر", "Egypt", "أفريقيا", "Africa"),
        ("JOR", "JO", "الأردن", "Jordan", "آسيا", "Asia"),
    };

    private async Task SeedCountriesAsync(CancellationToken ct)
    {
        foreach (var c in InitialCountries)
        {
            var id = DeterministicGuid.From($"country:{c.Iso3}");
            var exists = await _ctx.Countries.IgnoreQueryFilters()
                .AnyAsync(x => x.Id == id, ct).ConfigureAwait(false);
            if (exists) continue;

            var country = CCE.Domain.Country.Country.Register(
                c.Iso3, c.Iso2, c.NameAr, c.NameEn, c.RegionAr, c.RegionEn,
                $"https://flags.example.com/{c.Iso2.ToLowerInvariant()}.svg");
            // Override the auto-generated Id with the deterministic one.
            typeof(CCE.Domain.Country.Country).GetProperty(nameof(country.Id))!
                .SetValue(country, id);
            _ctx.Countries.Add(country);
        }
    }

    private static readonly (string Slug, string NameAr, string NameEn)[] InitialCategories =
    {
        ("solar", "الطاقة الشمسية", "Solar Energy"),
        ("wind", "طاقة الرياح", "Wind Energy"),
        ("storage", "التخزين", "Energy Storage"),
        ("hydrogen", "الهيدروجين", "Hydrogen"),
        ("efficiency", "كفاءة الطاقة", "Energy Efficiency"),
        ("policy", "السياسات", "Policy & Regulation"),
    };

    private async Task SeedResourceCategoriesAsync(CancellationToken ct)
    {
        for (var i = 0; i < InitialCategories.Length; i++)
        {
            var c = InitialCategories[i];
            var id = DeterministicGuid.From($"resource_category:{c.Slug}");
            var exists = await _ctx.ResourceCategories
                .AnyAsync(x => x.Id == id, ct).ConfigureAwait(false);
            if (exists) continue;
            var cat = ResourceCategory.Create(c.NameAr, c.NameEn, c.Slug, parentId: null, orderIndex: i);
            typeof(ResourceCategory).GetProperty(nameof(cat.Id))!.SetValue(cat, id);
            _ctx.ResourceCategories.Add(cat);
        }
    }

    private static readonly (string Slug, string NameAr, string NameEn,
        string DescriptionAr, string DescriptionEn)[] InitialTopics =
    {
        ("general", "عام", "General", "نقاشات عامة", "General discussions"),
        ("solar-power", "الطاقة الشمسية", "Solar Power", "كل ما يخص الطاقة الشمسية", "All about solar power"),
        ("policy", "السياسات", "Policy", "السياسات والتشريعات", "Policy and regulation"),
        ("research", "الأبحاث", "Research", "الأبحاث الحديثة", "Latest research"),
    };

    private async Task SeedTopicsAsync(CancellationToken ct)
    {
        for (var i = 0; i < InitialTopics.Length; i++)
        {
            var t = InitialTopics[i];
            var id = DeterministicGuid.From($"topic:{t.Slug}");
            var exists = await _ctx.Topics
                .AnyAsync(x => x.Id == id, ct).ConfigureAwait(false);
            if (exists) continue;
            var topic = Topic.Create(t.NameAr, t.NameEn, t.DescriptionAr, t.DescriptionEn,
                t.Slug, parentId: null, iconUrl: null, orderIndex: i);
            typeof(Topic).GetProperty(nameof(topic.Id))!.SetValue(topic, id);
            _ctx.Topics.Add(topic);
        }
    }
}
```

- [ ] **Step 2: Tests**

```csharp
using CCE.Infrastructure.Persistence;
using CCE.Seeder;
using CCE.Seeder.Seeders;
using CCE.TestInfrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CCE.Infrastructure.Tests.Seeder;

public class ReferenceDataSeederTests
{
    private static (CceDbContext Ctx, ReferenceDataSeeder Seeder) Build()
    {
        var ctx = new CceDbContext(new DbContextOptionsBuilder<CceDbContext>()
            .UseInMemoryDatabase(System.Guid.NewGuid().ToString())
            .Options);
        var seeder = new ReferenceDataSeeder(ctx, new FakeSystemClock(),
            NullLogger<ReferenceDataSeeder>.Instance);
        return (ctx, seeder);
    }

    [Fact]
    public async Task First_run_seeds_countries_categories_topics()
    {
        var (ctx, seeder) = Build();
        await seeder.SeedAsync();
        (await ctx.Countries.CountAsync()).Should().BeGreaterThan(5);
        (await ctx.ResourceCategories.CountAsync()).Should().BeGreaterThan(3);
        (await ctx.Topics.CountAsync()).Should().BeGreaterThan(3);
    }

    [Fact]
    public async Task Saudi_Arabia_seeded()
    {
        var (ctx, seeder) = Build();
        await seeder.SeedAsync();
        var saudi = await ctx.Countries.FirstOrDefaultAsync(c => c.IsoAlpha3 == "SAU");
        saudi.Should().NotBeNull();
        saudi!.NameEn.Should().Be("Saudi Arabia");
    }

    [Fact]
    public async Task Second_run_is_idempotent()
    {
        var (ctx, seeder) = Build();
        await seeder.SeedAsync();
        var firstCountries = await ctx.Countries.CountAsync();
        await seeder.SeedAsync();
        var secondCountries = await ctx.Countries.CountAsync();
        secondCountries.Should().Be(firstCountries);
    }
}
```

- [ ] **Step 3: Build + run + commit**

```bash
dotnet build backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj --nologo --no-restore 2>&1 | tail -5
dotnet test backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj --nologo --no-build --no-restore --filter "FullyQualifiedName~ReferenceDataSeederTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
git add backend/src/CCE.Seeder/Seeders/ReferenceDataSeeder.cs backend/tests/CCE.Infrastructure.Tests/Seeder/ReferenceDataSeederTests.cs
git -c commit.gpgsign=false commit -m "feat(seeder): ReferenceDataSeeder part 1 — countries + resource-categories + topics (3 TDD tests)"
```

---

## Task 9.4: `ReferenceDataSeeder` part 2 — CityTechnologies + NotificationTemplates + Pages + HomepageSections

**Files:**
- Modify: `backend/src/CCE.Seeder/Seeders/ReferenceDataSeeder.cs` (extend)

- [ ] **Step 1: Add private helpers + chain into `SeedAsync`**

Append to `ReferenceDataSeeder.cs`:

```csharp
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedCountriesAsync(cancellationToken).ConfigureAwait(false);
        await SeedResourceCategoriesAsync(cancellationToken).ConfigureAwait(false);
        await SeedTopicsAsync(cancellationToken).ConfigureAwait(false);
        await SeedCityTechnologiesAsync(cancellationToken).ConfigureAwait(false);
        await SeedNotificationTemplatesAsync(cancellationToken).ConfigureAwait(false);
        await SeedStaticPagesAsync(cancellationToken).ConfigureAwait(false);
        await SeedHomepageSectionsAsync(cancellationToken).ConfigureAwait(false);
        await _ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
```

(Replace the existing `SeedAsync` with the version above.)

Then add the four new methods at the bottom of the class:

```csharp
    private static readonly (string Slug, string NameAr, string NameEn,
        string DescriptionAr, string DescriptionEn,
        string CategoryAr, string CategoryEn,
        decimal CarbonImpact, decimal Cost)[] InitialCityTechs =
    {
        ("solar-rooftop", "ألواح شمسية على الأسطح", "Rooftop Solar Panels",
         "نظام كهروضوئي سكني بقدرة 5 ك.و", "5kW residential PV system",
         "الطاقة المتجددة", "Renewable Energy", -2500m, 12000m),
        ("ev-charging", "شواحن السيارات الكهربائية", "EV Charging Stations",
         "محطات شحن سريعة للسيارات الكهربائية", "Fast-charging stations",
         "النقل", "Transportation", -1800m, 8000m),
        ("led-lighting", "إنارة LED", "LED Lighting",
         "ترقية شاملة لإنارة LED", "Building-wide LED retrofit",
         "كفاءة الطاقة", "Energy Efficiency", -500m, 3000m),
        ("heat-pump", "مضخة حرارية", "Heat Pump",
         "مضخة حرارية للتدفئة والتبريد", "HVAC heat-pump system",
         "كفاءة الطاقة", "Energy Efficiency", -1200m, 7500m),
    };

    private async Task SeedCityTechnologiesAsync(CancellationToken ct)
    {
        foreach (var t in InitialCityTechs)
        {
            var id = DeterministicGuid.From($"city_tech:{t.Slug}");
            var exists = await _ctx.CityTechnologies
                .AnyAsync(x => x.Id == id, ct).ConfigureAwait(false);
            if (exists) continue;
            var tech = CCE.Domain.InteractiveCity.CityTechnology.Create(
                t.NameAr, t.NameEn, t.DescriptionAr, t.DescriptionEn,
                t.CategoryAr, t.CategoryEn, t.CarbonImpact, t.Cost);
            typeof(CCE.Domain.InteractiveCity.CityTechnology)
                .GetProperty(nameof(tech.Id))!.SetValue(tech, id);
            _ctx.CityTechnologies.Add(tech);
        }
    }

    private static readonly (string Code, string SubjectAr, string SubjectEn,
        string BodyAr, string BodyEn,
        CCE.Domain.Notifications.NotificationChannel Channel)[] InitialTemplates =
    {
        ("ACCOUNT_CREATED", "تم إنشاء حسابك", "Your account is created",
         "مرحباً {{Name}}، تم إنشاء حسابك بنجاح.", "Hi {{Name}}, your account is now active.",
         CCE.Domain.Notifications.NotificationChannel.Email),
        ("EXPERT_REQUEST_APPROVED", "تمت الموافقة على طلبك", "Your expert request was approved",
         "مرحباً {{Name}}، تمت الموافقة على طلب الخبير الخاص بك.",
         "Hi {{Name}}, your expert-registration request has been approved.",
         CCE.Domain.Notifications.NotificationChannel.Email),
        ("EXPERT_REQUEST_REJECTED", "تم رفض طلبك", "Your expert request was rejected",
         "نأسف، تم رفض طلب الخبير: {{Reason}}", "Sorry, your expert request was rejected: {{Reason}}",
         CCE.Domain.Notifications.NotificationChannel.Email),
        ("RESOURCE_REQUEST_APPROVED", "تمت الموافقة على المورد", "Country resource approved",
         "تمت الموافقة على مساهمة الدولة الخاصة بك.", "Your country resource submission was approved.",
         CCE.Domain.Notifications.NotificationChannel.InApp),
    };

    private async Task SeedNotificationTemplatesAsync(CancellationToken ct)
    {
        foreach (var t in InitialTemplates)
        {
            var id = DeterministicGuid.From($"template:{t.Code}");
            var exists = await _ctx.NotificationTemplates
                .AnyAsync(x => x.Id == id, ct).ConfigureAwait(false);
            if (exists) continue;
            var template = CCE.Domain.Notifications.NotificationTemplate.Define(
                t.Code, t.SubjectAr, t.SubjectEn, t.BodyAr, t.BodyEn, t.Channel, "{}");
            typeof(CCE.Domain.Notifications.NotificationTemplate)
                .GetProperty(nameof(template.Id))!.SetValue(template, id);
            _ctx.NotificationTemplates.Add(template);
        }
    }

    private static readonly (string Slug, CCE.Domain.Content.PageType Type,
        string TitleAr, string TitleEn, string ContentAr, string ContentEn)[] InitialPages =
    {
        ("about", CCE.Domain.Content.PageType.AboutPlatform,
         "عن المنصة", "About the Platform",
         "<p>منصة المعرفة للاقتصاد الكربوني الدائري...</p>",
         "<p>The Circular Carbon Economy Knowledge Center...</p>"),
        ("terms", CCE.Domain.Content.PageType.TermsOfService,
         "شروط الاستخدام", "Terms of Service",
         "<p>تطبق شروط الاستخدام التالية...</p>",
         "<p>The following terms of service apply...</p>"),
        ("privacy", CCE.Domain.Content.PageType.PrivacyPolicy,
         "سياسة الخصوصية", "Privacy Policy",
         "<p>نلتزم بحماية بياناتك الشخصية...</p>",
         "<p>We are committed to protecting your data...</p>"),
    };

    private async Task SeedStaticPagesAsync(CancellationToken ct)
    {
        foreach (var p in InitialPages)
        {
            var id = DeterministicGuid.From($"page:{p.Type}:{p.Slug}");
            var exists = await _ctx.Pages.IgnoreQueryFilters()
                .AnyAsync(x => x.Id == id, ct).ConfigureAwait(false);
            if (exists) continue;
            var page = CCE.Domain.Content.Page.Create(
                p.Slug, p.Type, p.TitleAr, p.TitleEn, p.ContentAr, p.ContentEn);
            typeof(CCE.Domain.Content.Page)
                .GetProperty(nameof(page.Id))!.SetValue(page, id);
            _ctx.Pages.Add(page);
        }
    }

    private static readonly (CCE.Domain.Content.HomepageSectionType Type, int Order,
        string ContentAr, string ContentEn)[] InitialSections =
    {
        (CCE.Domain.Content.HomepageSectionType.Hero, 0,
         "{\"titleAr\": \"معاً نحو اقتصاد كربوني دائري\"}",
         "{\"titleEn\": \"Together towards a circular carbon economy\"}"),
        (CCE.Domain.Content.HomepageSectionType.FeaturedNews, 1, "{}", "{}"),
        (CCE.Domain.Content.HomepageSectionType.FeaturedResources, 2, "{}", "{}"),
        (CCE.Domain.Content.HomepageSectionType.UpcomingEvents, 3, "{}", "{}"),
        (CCE.Domain.Content.HomepageSectionType.NewsletterSignup, 4, "{}", "{}"),
    };

    private async Task SeedHomepageSectionsAsync(CancellationToken ct)
    {
        foreach (var s in InitialSections)
        {
            var id = DeterministicGuid.From($"homepage_section:{s.Type}:{s.Order}");
            var exists = await _ctx.HomepageSections.IgnoreQueryFilters()
                .AnyAsync(x => x.Id == id, ct).ConfigureAwait(false);
            if (exists) continue;
            var section = CCE.Domain.Content.HomepageSection.Create(
                s.Type, s.Order, s.ContentAr, s.ContentEn);
            typeof(CCE.Domain.Content.HomepageSection)
                .GetProperty(nameof(section.Id))!.SetValue(section, id);
            _ctx.HomepageSections.Add(section);
        }
    }
```

- [ ] **Step 2: Update tests** — append assertions to existing `ReferenceDataSeederTests`:

Add tests:

```csharp
    [Fact]
    public async Task City_technologies_seeded()
    {
        var (ctx, seeder) = Build();
        await seeder.SeedAsync();
        (await ctx.CityTechnologies.CountAsync()).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Notification_templates_seeded()
    {
        var (ctx, seeder) = Build();
        await seeder.SeedAsync();
        var account = await ctx.NotificationTemplates.FirstOrDefaultAsync(t => t.Code == "ACCOUNT_CREATED");
        account.Should().NotBeNull();
    }

    [Fact]
    public async Task Static_pages_seeded()
    {
        var (ctx, seeder) = Build();
        await seeder.SeedAsync();
        (await ctx.Pages.CountAsync()).Should().Be(3);
    }

    [Fact]
    public async Task Homepage_sections_seeded()
    {
        var (ctx, seeder) = Build();
        await seeder.SeedAsync();
        (await ctx.HomepageSections.CountAsync()).Should().BeGreaterThanOrEqualTo(5);
    }
```

- [ ] **Step 3: Build + run + commit**

```bash
dotnet test backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~ReferenceDataSeederTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
git add backend/src/CCE.Seeder/Seeders/ReferenceDataSeeder.cs backend/tests/CCE.Infrastructure.Tests/Seeder/ReferenceDataSeederTests.cs
git -c commit.gpgsign=false commit -m "feat(seeder): ReferenceDataSeeder part 2 — city techs + notification templates + static pages + homepage sections (4 TDD tests)"
```

---

## Task 9.5: `KnowledgeMapSeeder`

**Files:**
- Create: `backend/src/CCE.Seeder/Seeders/KnowledgeMapSeeder.cs`
- Create: `backend/tests/CCE.Infrastructure.Tests/Seeder/KnowledgeMapSeederTests.cs`

**Rationale:** A small starter knowledge map ("CCE basics") with 4 nodes + 3 edges, demonstrating the Technology/Sector/SubTopic vocabulary. Lives in its own seeder because the wiring is complex enough to deserve its own file.

- [ ] **Step 1: Seeder**

```csharp
using CCE.Domain.KnowledgeMaps;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CCE.Seeder.Seeders;

public sealed class KnowledgeMapSeeder : ISeeder
{
    private readonly CceDbContext _ctx;
    private readonly ILogger<KnowledgeMapSeeder> _logger;

    public KnowledgeMapSeeder(CceDbContext ctx, ILogger<KnowledgeMapSeeder> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }

    public int Order => 30;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var mapId = DeterministicGuid.From("knowledge_map:cce-basics");
        var mapExists = await _ctx.KnowledgeMaps.IgnoreQueryFilters()
            .AnyAsync(m => m.Id == mapId, cancellationToken).ConfigureAwait(false);
        if (!mapExists)
        {
            var map = KnowledgeMap.Create(
                "أساسيات الاقتصاد الكربوني الدائري",
                "Circular Carbon Economy Basics",
                "خريطة معرفية تشرح المبادئ الأساسية",
                "Knowledge map of the four R's", "cce-basics");
            typeof(KnowledgeMap).GetProperty(nameof(map.Id))!.SetValue(map, mapId);
            _ctx.KnowledgeMaps.Add(map);
        }

        var nodes = new[]
        {
            ("reduce", "تقليل", "Reduce", NodeType.Sector, 100.0, 100.0, 0),
            ("reuse", "إعادة استخدام", "Reuse", NodeType.Sector, 300.0, 100.0, 1),
            ("recycle", "إعادة تدوير", "Recycle", NodeType.Sector, 100.0, 300.0, 2),
            ("remove", "إزالة الكربون", "Remove", NodeType.Sector, 300.0, 300.0, 3),
        };

        var nodeIds = new Dictionary<string, System.Guid>();
        foreach (var (slug, nameAr, nameEn, type, x, y, order) in nodes)
        {
            var id = DeterministicGuid.From($"km_node:cce-basics:{slug}");
            nodeIds[slug] = id;
            var exists = await _ctx.KnowledgeMapNodes
                .AnyAsync(n => n.Id == id, cancellationToken).ConfigureAwait(false);
            if (exists) continue;
            var node = KnowledgeMapNode.Create(mapId, nameAr, nameEn, type, null, null, null, x, y, order);
            typeof(KnowledgeMapNode).GetProperty(nameof(node.Id))!.SetValue(node, id);
            _ctx.KnowledgeMapNodes.Add(node);
        }

        var edges = new[]
        {
            ("reduce", "reuse", RelationshipType.RelatedTo),
            ("reuse", "recycle", RelationshipType.RelatedTo),
            ("recycle", "remove", RelationshipType.RelatedTo),
        };

        for (var i = 0; i < edges.Length; i++)
        {
            var (from, to, rel) = edges[i];
            var id = DeterministicGuid.From($"km_edge:cce-basics:{from}-{to}-{rel}");
            var exists = await _ctx.KnowledgeMapEdges
                .AnyAsync(e => e.Id == id, cancellationToken).ConfigureAwait(false);
            if (exists) continue;
            var edge = KnowledgeMapEdge.Connect(mapId, nodeIds[from], nodeIds[to], rel, i);
            typeof(KnowledgeMapEdge).GetProperty(nameof(edge.Id))!.SetValue(edge, id);
            _ctx.KnowledgeMapEdges.Add(edge);
        }

        await _ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
```

- [ ] **Step 2: Tests**

```csharp
using CCE.Infrastructure.Persistence;
using CCE.Seeder.Seeders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CCE.Infrastructure.Tests.Seeder;

public class KnowledgeMapSeederTests
{
    private static (CceDbContext Ctx, KnowledgeMapSeeder Seeder) Build()
    {
        var ctx = new CceDbContext(new DbContextOptionsBuilder<CceDbContext>()
            .UseInMemoryDatabase(System.Guid.NewGuid().ToString())
            .Options);
        return (ctx, new KnowledgeMapSeeder(ctx, NullLogger<KnowledgeMapSeeder>.Instance));
    }

    [Fact]
    public async Task Seeds_one_map_with_4_nodes_and_3_edges()
    {
        var (ctx, seeder) = Build();
        await seeder.SeedAsync();
        (await ctx.KnowledgeMaps.CountAsync()).Should().Be(1);
        (await ctx.KnowledgeMapNodes.CountAsync()).Should().Be(4);
        (await ctx.KnowledgeMapEdges.CountAsync()).Should().Be(3);
    }

    [Fact]
    public async Task Re_running_does_not_duplicate()
    {
        var (ctx, seeder) = Build();
        await seeder.SeedAsync();
        await seeder.SeedAsync();
        (await ctx.KnowledgeMapNodes.CountAsync()).Should().Be(4);
        (await ctx.KnowledgeMapEdges.CountAsync()).Should().Be(3);
    }
}
```

- [ ] **Step 3: Build + run + commit**

```bash
dotnet test backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~KnowledgeMapSeederTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
git add backend/src/CCE.Seeder/Seeders/KnowledgeMapSeeder.cs backend/tests/CCE.Infrastructure.Tests/Seeder/KnowledgeMapSeederTests.cs
git -c commit.gpgsign=false commit -m "feat(seeder): KnowledgeMapSeeder (CCE-basics map: 4 nodes + 3 edges, idempotent) (2 TDD tests)"
```

---

## Task 9.6: `DemoDataSeeder` (`--demo` only)

**Files:**
- Create: `backend/src/CCE.Seeder/Seeders/DemoDataSeeder.cs`
- Create: `backend/tests/CCE.Infrastructure.Tests/Seeder/DemoDataSeederTests.cs`

**Rationale:** Optional sample data — a few news articles, events, and posts for the dev/demo environment. Skipped in prod by `SeedRunner`'s `includeDemo` flag.

- [ ] **Step 1: Seeder**

```csharp
using CCE.Domain.Common;
using CCE.Domain.Content;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CCE.Seeder.Seeders;

public sealed class DemoDataSeeder : ISeeder
{
    private readonly CceDbContext _ctx;
    private readonly ISystemClock _clock;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(CceDbContext ctx, ISystemClock clock, ILogger<DemoDataSeeder> logger)
    {
        _ctx = ctx;
        _clock = clock;
        _logger = logger;
    }

    public int Order => 100;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedNewsAsync(cancellationToken).ConfigureAwait(false);
        await SeedEventsAsync(cancellationToken).ConfigureAwait(false);
        await _ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static readonly System.Guid SystemAuthorId =
        DeterministicGuid.From("user:system_demo_author");

    private static readonly (string Slug, string TitleAr, string TitleEn,
        string ContentAr, string ContentEn)[] DemoNews =
    {
        ("welcome", "أهلاً بكم في منصة المعرفة", "Welcome to the Knowledge Center",
         "<p>منصة جديدة لمشاركة المعرفة...</p>", "<p>A new platform for sharing knowledge...</p>"),
        ("solar-milestone", "إنجاز جديد في الطاقة الشمسية", "New Solar Milestone",
         "<p>تم تجاوز رقم قياسي...</p>", "<p>A new world record was set...</p>"),
    };

    private async Task SeedNewsAsync(CancellationToken ct)
    {
        foreach (var n in DemoNews)
        {
            var id = DeterministicGuid.From($"news:{n.Slug}");
            var exists = await _ctx.News.IgnoreQueryFilters()
                .AnyAsync(x => x.Id == id, ct).ConfigureAwait(false);
            if (exists) continue;
            var news = News.Draft(n.TitleAr, n.TitleEn, n.ContentAr, n.ContentEn,
                n.Slug, SystemAuthorId, featuredImageUrl: null, _clock);
            typeof(News).GetProperty(nameof(news.Id))!.SetValue(news, id);
            news.Publish(_clock);
            _ctx.News.Add(news);
        }
    }

    private async Task SeedEventsAsync(CancellationToken ct)
    {
        var startsOn = _clock.UtcNow.AddDays(30);
        var endsOn = startsOn.AddHours(2);
        var id = DeterministicGuid.From("event:demo:cce-conference");
        var exists = await _ctx.Events.IgnoreQueryFilters()
            .AnyAsync(x => x.Id == id, ct).ConfigureAwait(false);
        if (exists) return;
        var ev = CCE.Domain.Content.Event.Schedule(
            "مؤتمر CCE السنوي", "CCE Annual Conference",
            "نقاش حول مستقبل الاقتصاد الكربوني", "Discussion on the future of CCE",
            startsOn, endsOn, "الرياض", "Riyadh",
            null, null, _clock);
        typeof(CCE.Domain.Content.Event).GetProperty(nameof(ev.Id))!.SetValue(ev, id);
        _ctx.Events.Add(ev);
    }
}
```

- [ ] **Step 2: Tests**

```csharp
using CCE.Infrastructure.Persistence;
using CCE.Seeder.Seeders;
using CCE.TestInfrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CCE.Infrastructure.Tests.Seeder;

public class DemoDataSeederTests
{
    private static (CceDbContext Ctx, DemoDataSeeder Seeder) Build()
    {
        var ctx = new CceDbContext(new DbContextOptionsBuilder<CceDbContext>()
            .UseInMemoryDatabase(System.Guid.NewGuid().ToString())
            .Options);
        return (ctx, new DemoDataSeeder(ctx, new FakeSystemClock(),
            NullLogger<DemoDataSeeder>.Instance));
    }

    [Fact]
    public async Task Seeds_news_and_event()
    {
        var (ctx, seeder) = Build();
        await seeder.SeedAsync();
        (await ctx.News.CountAsync()).Should().BeGreaterThan(0);
        (await ctx.Events.CountAsync()).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task News_articles_are_published()
    {
        var (ctx, seeder) = Build();
        await seeder.SeedAsync();
        var allPublished = await ctx.News.AllAsync(n => n.PublishedOn != null);
        allPublished.Should().BeTrue();
    }

    [Fact]
    public async Task Idempotent()
    {
        var (ctx, seeder) = Build();
        await seeder.SeedAsync();
        var firstNews = await ctx.News.CountAsync();
        await seeder.SeedAsync();
        var secondNews = await ctx.News.CountAsync();
        secondNews.Should().Be(firstNews);
    }
}
```

- [ ] **Step 3: Build + run + commit**

```bash
dotnet test backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~DemoDataSeederTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
git add backend/src/CCE.Seeder/Seeders/DemoDataSeeder.cs backend/tests/CCE.Infrastructure.Tests/Seeder/DemoDataSeederTests.cs
git -c commit.gpgsign=false commit -m "feat(seeder): DemoDataSeeder (sample news + events behind --demo flag) (3 TDD tests)"
```

---

## Task 9.7: `SeedRunner` integration test (orders + opt-in/opt-out demo)

**Files:**
- Create: `backend/tests/CCE.Infrastructure.Tests/Seeder/SeedRunnerTests.cs`

- [ ] **Step 1: Tests**

```csharp
using CCE.Infrastructure.Persistence;
using CCE.Seeder;
using CCE.Seeder.Seeders;
using CCE.TestInfrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CCE.Infrastructure.Tests.Seeder;

public class SeedRunnerTests
{
    private static (CceDbContext Ctx, SeedRunner Runner) Build()
    {
        var ctx = new CceDbContext(new DbContextOptionsBuilder<CceDbContext>()
            .UseInMemoryDatabase(System.Guid.NewGuid().ToString())
            .Options);
        var clock = new FakeSystemClock();
        var seeders = new ISeeder[]
        {
            new RolesAndPermissionsSeeder(ctx, NullLogger<RolesAndPermissionsSeeder>.Instance),
            new ReferenceDataSeeder(ctx, clock, NullLogger<ReferenceDataSeeder>.Instance),
            new KnowledgeMapSeeder(ctx, NullLogger<KnowledgeMapSeeder>.Instance),
            new DemoDataSeeder(ctx, clock, NullLogger<DemoDataSeeder>.Instance),
        };
        var runner = new SeedRunner(seeders, NullLogger<SeedRunner>.Instance);
        return (ctx, runner);
    }

    [Fact]
    public async Task RunAll_without_demo_seeds_reference_only()
    {
        var (ctx, runner) = Build();
        await runner.RunAllAsync(includeDemo: false);

        (await ctx.Countries.CountAsync()).Should().BeGreaterThan(0);
        (await ctx.News.CountAsync()).Should().Be(0); // Demo skipped
    }

    [Fact]
    public async Task RunAll_with_demo_seeds_demo_data_too()
    {
        var (ctx, runner) = Build();
        await runner.RunAllAsync(includeDemo: true);

        (await ctx.News.CountAsync()).Should().BeGreaterThan(0);
    }
}
```

- [ ] **Step 2: Run + commit**

```bash
dotnet test backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~SeedRunnerTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
git add backend/tests/CCE.Infrastructure.Tests/Seeder/SeedRunnerTests.cs
git -c commit.gpgsign=false commit -m "test(seeder): SeedRunner orchestration + demo opt-in (2 tests)"
```

---

## Task 9.8: Phase 09 close

- [ ] **Step 1: Full backend test run**

```bash
dotnet test backend/CCE.sln --nologo --no-restore --logger "console;verbosity=minimal" 2>&1 | grep -E "(Passed!|Failed!|Skipped!)"
```

Expected: 5 result lines, all `Passed!`. Infrastructure.Tests grew by ~17 (3 + 7 + 2 + 3 + 2). Total ~362.

- [ ] **Step 2: Update progress doc**

Mark Phase 09 ✅ Done. Use the actual numbers reported.

- [ ] **Step 3: Commit**

```bash
git add docs/subprojects/02-data-domain-progress.md
git -c commit.gpgsign=false commit -m "docs(sub-2): mark Phase 09 done; idempotent seeders shipped (roles + reference + KM + demo)"
```

---

## Phase 09 — completion checklist

- [ ] `CCE.Seeder` library exists with `ISeeder` + `SeedRunner` + `DeterministicGuid`.
- [ ] `RolesAndPermissionsSeeder` creates 5 roles + permission claims, idempotent.
- [ ] `ReferenceDataSeeder` populates countries + categories + topics + city techs + templates + pages + homepage sections.
- [ ] `KnowledgeMapSeeder` ships the CCE-basics knowledge map (4 nodes + 3 edges).
- [ ] `DemoDataSeeder` adds sample news + events (only when `--demo`).
- [ ] All seeders are idempotent (verified by tests).
- [ ] `SeedRunner.RunAllAsync(includeDemo: false)` skips demo data.
- [ ] All Phase 08 regression tests still pass.
- [ ] 8 new commits.

**If all boxes ticked, Phase 09 is complete. Proceed to Phase 10 (Architecture tests + ADRs + release tag).**
