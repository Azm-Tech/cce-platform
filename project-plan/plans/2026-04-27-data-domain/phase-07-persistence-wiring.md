# Phase 07 — Persistence wiring

> Parent: [`../2026-04-27-data-domain.md`](../2026-04-27-data-domain.md) · Spec §3.2, §5.3–§5.5

**Phase goal:** Wire the 36 Domain entities into EF Core. Promote `CceDbContext` from `DbContext` to `IdentityDbContext<User, Role, Guid>`, register `DbSet<T>` for every entity, register a single soft-delete query filter via reflection, write `IEntityTypeConfiguration<T>` for entities that need indexes / JSON columns / RowVersion / cascade tweaks, and ship the three core infrastructure services: `AuditingInterceptor`, `DomainEventDispatcher`, `DbExceptionMapper`.

**Tasks in this phase:** 9
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 06 closed (`b0ecc69` HEAD); 340 backend tests passing.

**Note on migrations:** Phase 07 is code-only. No `dotnet ef migrations add` here — that lives in Phase 08.

---

## Task 7.1: `CceDbContext` rewrite (IdentityDbContext + DbSets + soft-delete filter)

**Files:**
- Modify: `backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj` (add Identity.EFCore reference)
- Modify: `backend/src/CCE.Infrastructure/Persistence/CceDbContext.cs`
- Modify: `backend/src/CCE.Application/Common/Interfaces/ICceDbContext.cs` (no signature change needed; verify still implemented)
- Modify: `backend/src/CCE.Infrastructure/Persistence/CceDbContextDesignTimeFactory.cs` if needed (existing should still work)

- [ ] **Step 1: Add Identity.EFCore package reference**

`backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj` — add to first `ItemGroup` of PackageReferences:

```xml
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" />
```

(CPM already pins 8.0.10.)

- [ ] **Step 2: Rewrite `CceDbContext.cs`**

```csharp
using System.Linq.Expressions;
using System.Reflection;
using CCE.Application.Common.Interfaces;
using CCE.Domain.Audit;
using CCE.Domain.Common;
using CCE.Domain.Community;
using CCE.Domain.Content;
using CCE.Domain.Country;
using CCE.Domain.Identity;
using CCE.Domain.InteractiveCity;
using CCE.Domain.KnowledgeMaps;
using CCE.Domain.Notifications;
using CCE.Domain.Surveys;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Persistence;

/// <summary>
/// Application <see cref="DbContext"/>. Extends ASP.NET Identity's
/// <see cref="IdentityDbContext{User, Role, Guid}"/> so the Identity tables
/// (AspNetUsers/Roles/UserRoles/etc.) coexist with CCE entity tables in one model.
/// Snake-case naming via <c>EFCore.NamingConventions</c> applied in <c>DependencyInjection</c>.
/// </summary>
public sealed class CceDbContext
    : IdentityDbContext<User, Role, System.Guid>, ICceDbContext
{
    public CceDbContext(DbContextOptions<CceDbContext> options) : base(options) { }

    // ─── Audit (Foundation) ───
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    // ─── Identity bounded context ───
    public DbSet<StateRepresentativeAssignment> StateRepresentativeAssignments => Set<StateRepresentativeAssignment>();
    public DbSet<ExpertProfile> ExpertProfiles => Set<ExpertProfile>();
    public DbSet<ExpertRegistrationRequest> ExpertRegistrationRequests => Set<ExpertRegistrationRequest>();

    // ─── Content ───
    public DbSet<AssetFile> AssetFiles => Set<AssetFile>();
    public DbSet<ResourceCategory> ResourceCategories => Set<ResourceCategory>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<News> News => Set<News>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<HomepageSection> HomepageSections => Set<HomepageSection>();
    public DbSet<NewsletterSubscription> NewsletterSubscriptions => Set<NewsletterSubscription>();

    // ─── Country ───
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<CountryProfile> CountryProfiles => Set<CountryProfile>();
    public DbSet<CountryResourceRequest> CountryResourceRequests => Set<CountryResourceRequest>();
    public DbSet<CountryKapsarcSnapshot> CountryKapsarcSnapshots => Set<CountryKapsarcSnapshot>();

    // ─── Community ───
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostReply> PostReplies => Set<PostReply>();
    public DbSet<PostRating> PostRatings => Set<PostRating>();
    public DbSet<TopicFollow> TopicFollows => Set<TopicFollow>();
    public DbSet<UserFollow> UserFollows => Set<UserFollow>();
    public DbSet<PostFollow> PostFollows => Set<PostFollow>();

    // ─── Knowledge Maps ───
    public DbSet<KnowledgeMap> KnowledgeMaps => Set<KnowledgeMap>();
    public DbSet<KnowledgeMapNode> KnowledgeMapNodes => Set<KnowledgeMapNode>();
    public DbSet<KnowledgeMapEdge> KnowledgeMapEdges => Set<KnowledgeMapEdge>();
    public DbSet<KnowledgeMapAssociation> KnowledgeMapAssociations => Set<KnowledgeMapAssociation>();

    // ─── Interactive City ───
    public DbSet<CityScenario> CityScenarios => Set<CityScenario>();
    public DbSet<CityTechnology> CityTechnologies => Set<CityTechnology>();
    public DbSet<CityScenarioResult> CityScenarioResults => Set<CityScenarioResult>();

    // ─── Notifications ───
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();

    // ─── Surveys ───
    public DbSet<ServiceRating> ServiceRatings => Set<ServiceRating>();
    public DbSet<SearchQueryLog> SearchQueryLogs => Set<SearchQueryLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CceDbContext).Assembly);
        ApplySoftDeleteFilter(modelBuilder);
    }

    /// <summary>
    /// Spec §5.5: every <see cref="ISoftDeletable"/> entity gets a global query filter
    /// <c>HasQueryFilter(e =&gt; !e.IsDeleted)</c>. To bypass, use <c>IgnoreQueryFilters()</c>.
    /// </summary>
    private static void ApplySoftDeleteFilter(ModelBuilder modelBuilder)
    {
        var isDeletedProperty = typeof(ISoftDeletable).GetProperty(nameof(ISoftDeletable.IsDeleted))!;
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType)) continue;
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var body = Expression.Not(Expression.Property(parameter, isDeletedProperty));
            var lambda = Expression.Lambda(body, parameter);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }
}
```

- [ ] **Step 3: Restore + build**

```bash
dotnet restore backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj 2>&1 | tail -3
dotnet build backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj --nologo --no-restore 2>&1 | tail -8
```

Expected: 0 errors. (NU1608 may warn — already in NoWarn.)

If a CS error shows `User` isn't `IdentityUser<Guid>`, re-read the file: Phase 02's User correctly extends `IdentityUser<Guid>` so this should compile cleanly.

If `Microsoft.AspNetCore.Identity.EntityFrameworkCore` package can't be restored from local cache, download it via the technique used in Phase 02:

```bash
mkdir -p /tmp/local-nuget
for p in microsoft.aspnetcore.identity.entityframeworkcore microsoft.aspnetcore.identity; do
  curl --max-time 60 -sL -o /tmp/local-nuget/${p}.8.0.10.nupkg \
    https://www.nuget.org/api/v2/package/${p}/8.0.10
done
dotnet restore backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj \
  --source /tmp/local-nuget --source ~/.nuget/packages 2>&1 | tail -3
```

- [ ] **Step 4: Commit**

```bash
git add backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj backend/src/CCE.Infrastructure/Persistence/CceDbContext.cs
git -c commit.gpgsign=false commit -m "feat(persistence): CceDbContext extends IdentityDbContext<User,Role,Guid> + 36 DbSets + soft-delete filter"
```

---

## Task 7.2: Identity bounded-context configurations (5 entities)

**Files:**
- Create: `backend/src/CCE.Infrastructure/Persistence/Configurations/Identity/UserConfiguration.cs`
- Create: `backend/src/CCE.Infrastructure/Persistence/Configurations/Identity/RoleConfiguration.cs`
- Create: `backend/src/CCE.Infrastructure/Persistence/Configurations/Identity/StateRepresentativeAssignmentConfiguration.cs`
- Create: `backend/src/CCE.Infrastructure/Persistence/Configurations/Identity/ExpertProfileConfiguration.cs`
- Create: `backend/src/CCE.Infrastructure/Persistence/Configurations/Identity/ExpertRegistrationRequestConfiguration.cs`

- [ ] **Step 1: User configuration** (CCE-only columns; Identity columns stay default)

```csharp
using CCE.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Identity;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.LocalePreference).HasMaxLength(2).IsRequired();
        builder.Property(u => u.AvatarUrl).HasMaxLength(2048);
        builder.Property(u => u.Interests).HasColumnType("nvarchar(max)");
        builder.Property(u => u.KnowledgeLevel).HasConversion<int>();
        builder.HasIndex(u => u.CountryId).HasDatabaseName("ix_users_country_id");
    }
}
```

- [ ] **Step 2: Role configuration** (no extra columns; stub for completeness)

```csharp
using CCE.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Identity;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder) { /* defaults are fine */ }
}
```

- [ ] **Step 3: StateRepresentativeAssignment**

```csharp
using CCE.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Identity;

internal sealed class StateRepresentativeAssignmentConfiguration
    : IEntityTypeConfiguration<StateRepresentativeAssignment>
{
    public void Configure(EntityTypeBuilder<StateRepresentativeAssignment> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        // Filtered unique index: only one ACTIVE assignment per (UserId, CountryId).
        builder.HasIndex(a => new { a.UserId, a.CountryId })
               .IsUnique()
               .HasFilter("[is_deleted] = 0")
               .HasDatabaseName("ux_state_rep_active_user_country");
        builder.HasIndex(a => a.UserId).HasDatabaseName("ix_state_rep_user_id");
        builder.HasIndex(a => a.CountryId).HasDatabaseName("ix_state_rep_country_id");
    }
}
```

- [ ] **Step 4: ExpertProfile** (1:1 with User via UserId)

```csharp
using CCE.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Identity;

internal sealed class ExpertProfileConfiguration : IEntityTypeConfiguration<ExpertProfile>
{
    public void Configure(EntityTypeBuilder<ExpertProfile> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.BioAr).HasMaxLength(2000);
        builder.Property(p => p.BioEn).HasMaxLength(2000);
        builder.Property(p => p.AcademicTitleAr).HasMaxLength(128);
        builder.Property(p => p.AcademicTitleEn).HasMaxLength(128);
        builder.Property(p => p.ExpertiseTags).HasColumnType("nvarchar(max)");
        // 1:1: UserId is unique among non-deleted profiles
        builder.HasIndex(p => p.UserId)
               .IsUnique()
               .HasFilter("[is_deleted] = 0")
               .HasDatabaseName("ux_expert_profile_active_user");
    }
}
```

- [ ] **Step 5: ExpertRegistrationRequest**

```csharp
using CCE.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Identity;

internal sealed class ExpertRegistrationRequestConfiguration
    : IEntityTypeConfiguration<ExpertRegistrationRequest>
{
    public void Configure(EntityTypeBuilder<ExpertRegistrationRequest> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.RequestedBioAr).HasMaxLength(2000);
        builder.Property(r => r.RequestedBioEn).HasMaxLength(2000);
        builder.Property(r => r.RequestedTags).HasColumnType("nvarchar(max)");
        builder.Property(r => r.RejectionReasonAr).HasMaxLength(1000);
        builder.Property(r => r.RejectionReasonEn).HasMaxLength(1000);
        builder.Property(r => r.Status).HasConversion<int>();
        builder.HasIndex(r => r.RequestedById).HasDatabaseName("ix_expert_request_requested_by");
        builder.HasIndex(r => r.Status).HasDatabaseName("ix_expert_request_status");
        builder.Ignore(r => r.DomainEvents);
    }
}
```

- [ ] **Step 6: Build + commit**

```bash
dotnet build backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj --nologo --no-restore 2>&1 | tail -5
git add backend/src/CCE.Infrastructure/Persistence/Configurations/Identity/
git -c commit.gpgsign=false commit -m "feat(persistence): EF configurations for 5 Identity entities (filtered unique indexes for active assignments + 1:1 profile)"
```

---

## Task 7.3: Content + Country configurations (12 entities)

**Files:**
- Create: `backend/src/CCE.Infrastructure/Persistence/Configurations/Content/{ResourceCategoryConfiguration,AssetFileConfiguration,ResourceConfiguration,NewsConfiguration,EventConfiguration,PageConfiguration,HomepageSectionConfiguration,NewsletterSubscriptionConfiguration}.cs`
- Create: `backend/src/CCE.Infrastructure/Persistence/Configurations/Country/{CountryConfiguration,CountryProfileConfiguration,CountryResourceRequestConfiguration,CountryKapsarcSnapshotConfiguration}.cs`

- [ ] **Step 1: Content configurations**

`Content/ResourceCategoryConfiguration.cs`:

```csharp
using CCE.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Content;

internal sealed class ResourceCategoryConfiguration : IEntityTypeConfiguration<ResourceCategory>
{
    public void Configure(EntityTypeBuilder<ResourceCategory> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.NameAr).HasMaxLength(256).IsRequired();
        builder.Property(c => c.NameEn).HasMaxLength(256).IsRequired();
        builder.Property(c => c.Slug).HasMaxLength(128).IsRequired();
        builder.HasIndex(c => c.Slug).IsUnique().HasDatabaseName("ux_resource_category_slug");
        builder.HasIndex(c => c.ParentId).HasDatabaseName("ix_resource_category_parent_id");
    }
}
```

`Content/AssetFileConfiguration.cs`:

```csharp
using CCE.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Content;

internal sealed class AssetFileConfiguration : IEntityTypeConfiguration<AssetFile>
{
    public void Configure(EntityTypeBuilder<AssetFile> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Url).HasMaxLength(2048).IsRequired();
        builder.Property(a => a.OriginalFileName).HasMaxLength(512).IsRequired();
        builder.Property(a => a.MimeType).HasMaxLength(128).IsRequired();
        builder.Property(a => a.VirusScanStatus).HasConversion<int>();
        builder.HasIndex(a => a.VirusScanStatus).HasDatabaseName("ix_asset_file_scan_status");
    }
}
```

`Content/ResourceConfiguration.cs`:

```csharp
using CCE.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Content;

internal sealed class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.TitleAr).HasMaxLength(512).IsRequired();
        builder.Property(r => r.TitleEn).HasMaxLength(512).IsRequired();
        builder.Property(r => r.DescriptionAr).HasColumnType("nvarchar(max)");
        builder.Property(r => r.DescriptionEn).HasColumnType("nvarchar(max)");
        builder.Property(r => r.ResourceType).HasConversion<int>();
        builder.Property(r => r.RowVersion).IsRowVersion();
        builder.HasIndex(r => new { r.CategoryId, r.PublishedOn }).HasDatabaseName("ix_resource_category_published");
        builder.HasIndex(r => r.CountryId).HasDatabaseName("ix_resource_country_id");
        builder.HasIndex(r => r.AssetFileId).HasDatabaseName("ix_resource_asset_file_id");
        builder.Ignore(r => r.DomainEvents);
    }
}
```

`Content/NewsConfiguration.cs`:

```csharp
using CCE.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Content;

internal sealed class NewsConfiguration : IEntityTypeConfiguration<News>
{
    public void Configure(EntityTypeBuilder<News> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Property(n => n.TitleAr).HasMaxLength(512).IsRequired();
        builder.Property(n => n.TitleEn).HasMaxLength(512).IsRequired();
        builder.Property(n => n.ContentAr).HasColumnType("nvarchar(max)");
        builder.Property(n => n.ContentEn).HasColumnType("nvarchar(max)");
        builder.Property(n => n.Slug).HasMaxLength(256).IsRequired();
        builder.Property(n => n.FeaturedImageUrl).HasMaxLength(2048);
        builder.Property(n => n.RowVersion).IsRowVersion();
        builder.HasIndex(n => n.Slug)
               .IsUnique()
               .HasFilter("[is_deleted] = 0")
               .HasDatabaseName("ux_news_slug_active");
        builder.HasIndex(n => n.PublishedOn).HasDatabaseName("ix_news_published_on");
        builder.Ignore(n => n.DomainEvents);
    }
}
```

`Content/EventConfiguration.cs`:

```csharp
using CCE.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Content;

internal sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.TitleAr).HasMaxLength(512).IsRequired();
        builder.Property(e => e.TitleEn).HasMaxLength(512).IsRequired();
        builder.Property(e => e.DescriptionAr).HasColumnType("nvarchar(max)");
        builder.Property(e => e.DescriptionEn).HasColumnType("nvarchar(max)");
        builder.Property(e => e.LocationAr).HasMaxLength(512);
        builder.Property(e => e.LocationEn).HasMaxLength(512);
        builder.Property(e => e.OnlineMeetingUrl).HasMaxLength(2048);
        builder.Property(e => e.FeaturedImageUrl).HasMaxLength(2048);
        builder.Property(e => e.ICalUid).HasMaxLength(256).IsRequired();
        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.HasIndex(e => e.ICalUid).IsUnique().HasDatabaseName("ux_event_ical_uid");
        builder.HasIndex(e => e.StartsOn).HasDatabaseName("ix_event_starts_on");
        builder.Ignore(e => e.DomainEvents);
    }
}
```

`Content/PageConfiguration.cs`:

```csharp
using CCE.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Content;

internal sealed class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Slug).HasMaxLength(256).IsRequired();
        builder.Property(p => p.PageType).HasConversion<int>();
        builder.Property(p => p.TitleAr).HasMaxLength(512).IsRequired();
        builder.Property(p => p.TitleEn).HasMaxLength(512).IsRequired();
        builder.Property(p => p.ContentAr).HasColumnType("nvarchar(max)");
        builder.Property(p => p.ContentEn).HasColumnType("nvarchar(max)");
        builder.Property(p => p.RowVersion).IsRowVersion();
        builder.HasIndex(p => new { p.PageType, p.Slug })
               .IsUnique()
               .HasFilter("[is_deleted] = 0")
               .HasDatabaseName("ux_page_type_slug_active");
        builder.Ignore(p => p.DomainEvents);
    }
}
```

`Content/HomepageSectionConfiguration.cs`:

```csharp
using CCE.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Content;

internal sealed class HomepageSectionConfiguration : IEntityTypeConfiguration<HomepageSection>
{
    public void Configure(EntityTypeBuilder<HomepageSection> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.SectionType).HasConversion<int>();
        builder.Property(s => s.ContentAr).HasColumnType("nvarchar(max)");
        builder.Property(s => s.ContentEn).HasColumnType("nvarchar(max)");
        builder.HasIndex(s => new { s.IsActive, s.OrderIndex })
               .HasDatabaseName("ix_homepage_section_active_order");
    }
}
```

`Content/NewsletterSubscriptionConfiguration.cs`:

```csharp
using CCE.Domain.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Content;

internal sealed class NewsletterSubscriptionConfiguration : IEntityTypeConfiguration<NewsletterSubscription>
{
    public void Configure(EntityTypeBuilder<NewsletterSubscription> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Property(n => n.Email).HasMaxLength(320).IsRequired();
        builder.Property(n => n.LocalePreference).HasMaxLength(2).IsRequired();
        builder.Property(n => n.ConfirmationToken).HasMaxLength(64).IsRequired();
        builder.HasIndex(n => n.Email).IsUnique().HasDatabaseName("ux_newsletter_email");
        builder.HasIndex(n => n.ConfirmationToken).HasDatabaseName("ix_newsletter_token");
        builder.Ignore(n => n.DomainEvents);
    }
}
```

- [ ] **Step 2: Country configurations**

`Country/CountryConfiguration.cs`:

```csharp
using CCE.Domain.Country;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Country;

internal sealed class CountryConfiguration : IEntityTypeConfiguration<CCE.Domain.Country.Country>
{
    public void Configure(EntityTypeBuilder<CCE.Domain.Country.Country> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.IsoAlpha3).HasMaxLength(3).IsRequired();
        builder.Property(c => c.IsoAlpha2).HasMaxLength(2).IsRequired();
        builder.Property(c => c.NameAr).HasMaxLength(256).IsRequired();
        builder.Property(c => c.NameEn).HasMaxLength(256).IsRequired();
        builder.Property(c => c.RegionAr).HasMaxLength(128).IsRequired();
        builder.Property(c => c.RegionEn).HasMaxLength(128).IsRequired();
        builder.Property(c => c.FlagUrl).HasMaxLength(2048);
        builder.HasIndex(c => c.IsoAlpha3)
               .IsUnique()
               .HasFilter("[is_deleted] = 0")
               .HasDatabaseName("ux_country_iso_alpha3_active");
        builder.HasIndex(c => c.IsoAlpha2).HasDatabaseName("ix_country_iso_alpha2");
        builder.Ignore(c => c.DomainEvents);
    }
}
```

`Country/CountryProfileConfiguration.cs`:

```csharp
using CCE.Domain.Country;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Country;

internal sealed class CountryProfileConfiguration : IEntityTypeConfiguration<CountryProfile>
{
    public void Configure(EntityTypeBuilder<CountryProfile> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.DescriptionAr).HasColumnType("nvarchar(max)");
        builder.Property(p => p.DescriptionEn).HasColumnType("nvarchar(max)");
        builder.Property(p => p.KeyInitiativesAr).HasColumnType("nvarchar(max)");
        builder.Property(p => p.KeyInitiativesEn).HasColumnType("nvarchar(max)");
        builder.Property(p => p.ContactInfoAr).HasMaxLength(2000);
        builder.Property(p => p.ContactInfoEn).HasMaxLength(2000);
        builder.Property(p => p.RowVersion).IsRowVersion();
        builder.HasIndex(p => p.CountryId).IsUnique().HasDatabaseName("ux_country_profile_country_id");
    }
}
```

`Country/CountryResourceRequestConfiguration.cs`:

```csharp
using CCE.Domain.Country;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Country;

internal sealed class CountryResourceRequestConfiguration : IEntityTypeConfiguration<CountryResourceRequest>
{
    public void Configure(EntityTypeBuilder<CountryResourceRequest> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.ProposedTitleAr).HasMaxLength(512).IsRequired();
        builder.Property(r => r.ProposedTitleEn).HasMaxLength(512).IsRequired();
        builder.Property(r => r.ProposedDescriptionAr).HasColumnType("nvarchar(max)");
        builder.Property(r => r.ProposedDescriptionEn).HasColumnType("nvarchar(max)");
        builder.Property(r => r.AdminNotesAr).HasMaxLength(2000);
        builder.Property(r => r.AdminNotesEn).HasMaxLength(2000);
        builder.Property(r => r.ProposedResourceType).HasConversion<int>();
        builder.Property(r => r.Status).HasConversion<int>();
        builder.HasIndex(r => new { r.CountryId, r.Status }).HasDatabaseName("ix_country_request_country_status");
        builder.Ignore(r => r.DomainEvents);
    }
}
```

`Country/CountryKapsarcSnapshotConfiguration.cs`:

```csharp
using CCE.Domain.Country;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Country;

internal sealed class CountryKapsarcSnapshotConfiguration : IEntityTypeConfiguration<CountryKapsarcSnapshot>
{
    public void Configure(EntityTypeBuilder<CountryKapsarcSnapshot> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Classification).HasMaxLength(64).IsRequired();
        builder.Property(s => s.PerformanceScore).HasPrecision(5, 2);
        builder.Property(s => s.TotalIndex).HasPrecision(5, 2);
        builder.Property(s => s.SourceVersion).HasMaxLength(32);
        builder.HasIndex(s => new { s.CountryId, s.SnapshotTakenOn })
               .HasDatabaseName("ix_kapsarc_snapshot_country_taken");
    }
}
```

- [ ] **Step 3: Build + commit**

```bash
dotnet build backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj --nologo --no-restore 2>&1 | tail -5
git add backend/src/CCE.Infrastructure/Persistence/Configurations/Content/ backend/src/CCE.Infrastructure/Persistence/Configurations/Country/
git -c commit.gpgsign=false commit -m "feat(persistence): EF configurations for 8 Content + 4 Country entities (slugs, RowVersion, indexes)"
```

---

## Task 7.4: Community + KnowledgeMaps configurations (11 entities)

**Files:**
- Create: `backend/src/CCE.Infrastructure/Persistence/Configurations/Community/{TopicConfiguration,PostConfiguration,PostReplyConfiguration,PostRatingConfiguration,TopicFollowConfiguration,UserFollowConfiguration,PostFollowConfiguration}.cs`
- Create: `backend/src/CCE.Infrastructure/Persistence/Configurations/KnowledgeMaps/{KnowledgeMapConfiguration,KnowledgeMapNodeConfiguration,KnowledgeMapEdgeConfiguration,KnowledgeMapAssociationConfiguration}.cs`

- [ ] **Step 1: Community configurations**

`Community/TopicConfiguration.cs`:

```csharp
using CCE.Domain.Community;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Community;

internal sealed class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.NameAr).HasMaxLength(256).IsRequired();
        builder.Property(t => t.NameEn).HasMaxLength(256).IsRequired();
        builder.Property(t => t.Slug).HasMaxLength(128).IsRequired();
        builder.Property(t => t.IconUrl).HasMaxLength(2048);
        builder.HasIndex(t => t.Slug)
               .IsUnique()
               .HasFilter("[is_deleted] = 0")
               .HasDatabaseName("ux_topic_slug_active");
    }
}
```

`Community/PostConfiguration.cs`:

```csharp
using CCE.Domain.Community;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Community;

internal sealed class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Content).HasMaxLength(8000).IsRequired();
        builder.Property(p => p.Locale).HasMaxLength(2).IsRequired();
        builder.HasIndex(p => p.TopicId).HasDatabaseName("ix_post_topic_id");
        builder.HasIndex(p => new { p.AuthorId, p.CreatedOn }).HasDatabaseName("ix_post_author_created");
        builder.Ignore(p => p.DomainEvents);
    }
}
```

`Community/PostReplyConfiguration.cs`:

```csharp
using CCE.Domain.Community;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Community;

internal sealed class PostReplyConfiguration : IEntityTypeConfiguration<PostReply>
{
    public void Configure(EntityTypeBuilder<PostReply> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Content).HasMaxLength(8000).IsRequired();
        builder.Property(r => r.Locale).HasMaxLength(2).IsRequired();
        builder.HasIndex(r => r.PostId).HasDatabaseName("ix_post_reply_post_id");
        builder.HasIndex(r => r.ParentReplyId).HasDatabaseName("ix_post_reply_parent_id");
    }
}
```

`Community/PostRatingConfiguration.cs`:

```csharp
using CCE.Domain.Community;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Community;

internal sealed class PostRatingConfiguration : IEntityTypeConfiguration<PostRating>
{
    public void Configure(EntityTypeBuilder<PostRating> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.HasIndex(r => new { r.PostId, r.UserId }).IsUnique().HasDatabaseName("ux_post_rating_post_user");
    }
}
```

`Community/TopicFollowConfiguration.cs`:

```csharp
using CCE.Domain.Community;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Community;

internal sealed class TopicFollowConfiguration : IEntityTypeConfiguration<TopicFollow>
{
    public void Configure(EntityTypeBuilder<TopicFollow> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();
        builder.HasIndex(f => new { f.TopicId, f.UserId }).IsUnique().HasDatabaseName("ux_topic_follow_topic_user");
    }
}
```

`Community/UserFollowConfiguration.cs`:

```csharp
using CCE.Domain.Community;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Community;

internal sealed class UserFollowConfiguration : IEntityTypeConfiguration<UserFollow>
{
    public void Configure(EntityTypeBuilder<UserFollow> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();
        builder.HasIndex(f => new { f.FollowerId, f.FollowedId })
               .IsUnique()
               .HasDatabaseName("ux_user_follow_follower_followed");
    }
}
```

`Community/PostFollowConfiguration.cs`:

```csharp
using CCE.Domain.Community;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Community;

internal sealed class PostFollowConfiguration : IEntityTypeConfiguration<PostFollow>
{
    public void Configure(EntityTypeBuilder<PostFollow> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();
        builder.HasIndex(f => new { f.PostId, f.UserId }).IsUnique().HasDatabaseName("ux_post_follow_post_user");
    }
}
```

- [ ] **Step 2: KnowledgeMaps configurations**

`KnowledgeMaps/KnowledgeMapConfiguration.cs`:

```csharp
using CCE.Domain.KnowledgeMaps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.KnowledgeMaps;

internal sealed class KnowledgeMapConfiguration : IEntityTypeConfiguration<KnowledgeMap>
{
    public void Configure(EntityTypeBuilder<KnowledgeMap> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.NameAr).HasMaxLength(256).IsRequired();
        builder.Property(m => m.NameEn).HasMaxLength(256).IsRequired();
        builder.Property(m => m.Slug).HasMaxLength(128).IsRequired();
        builder.Property(m => m.RowVersion).IsRowVersion();
        builder.HasIndex(m => m.Slug)
               .IsUnique()
               .HasFilter("[is_deleted] = 0")
               .HasDatabaseName("ux_knowledge_map_slug_active");
        builder.Ignore(m => m.DomainEvents);
    }
}
```

`KnowledgeMaps/KnowledgeMapNodeConfiguration.cs`:

```csharp
using CCE.Domain.KnowledgeMaps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.KnowledgeMaps;

internal sealed class KnowledgeMapNodeConfiguration : IEntityTypeConfiguration<KnowledgeMapNode>
{
    public void Configure(EntityTypeBuilder<KnowledgeMapNode> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Property(n => n.NameAr).HasMaxLength(256).IsRequired();
        builder.Property(n => n.NameEn).HasMaxLength(256).IsRequired();
        builder.Property(n => n.NodeType).HasConversion<int>();
        builder.Property(n => n.IconUrl).HasMaxLength(2048);
        builder.HasIndex(n => new { n.MapId, n.OrderIndex }).HasDatabaseName("ix_km_node_map_order");
    }
}
```

`KnowledgeMaps/KnowledgeMapEdgeConfiguration.cs`:

```csharp
using CCE.Domain.KnowledgeMaps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.KnowledgeMaps;

internal sealed class KnowledgeMapEdgeConfiguration : IEntityTypeConfiguration<KnowledgeMapEdge>
{
    public void Configure(EntityTypeBuilder<KnowledgeMapEdge> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.RelationshipType).HasConversion<int>();
        builder.HasIndex(e => new { e.MapId, e.FromNodeId, e.ToNodeId, e.RelationshipType })
               .IsUnique()
               .HasDatabaseName("ux_km_edge_map_from_to_relation");
        builder.HasIndex(e => e.FromNodeId).HasDatabaseName("ix_km_edge_from_node");
        builder.HasIndex(e => e.ToNodeId).HasDatabaseName("ix_km_edge_to_node");
    }
}
```

`KnowledgeMaps/KnowledgeMapAssociationConfiguration.cs`:

```csharp
using CCE.Domain.KnowledgeMaps;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.KnowledgeMaps;

internal sealed class KnowledgeMapAssociationConfiguration : IEntityTypeConfiguration<KnowledgeMapAssociation>
{
    public void Configure(EntityTypeBuilder<KnowledgeMapAssociation> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.AssociatedType).HasConversion<int>();
        builder.HasIndex(a => new { a.NodeId, a.AssociatedType, a.AssociatedId })
               .IsUnique()
               .HasDatabaseName("ux_km_assoc_node_type_id");
    }
}
```

- [ ] **Step 3: Build + commit**

```bash
dotnet build backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj --nologo --no-restore 2>&1 | tail -5
git add backend/src/CCE.Infrastructure/Persistence/Configurations/Community/ backend/src/CCE.Infrastructure/Persistence/Configurations/KnowledgeMaps/
git -c commit.gpgsign=false commit -m "feat(persistence): EF configurations for 7 Community + 4 KnowledgeMap entities (composite uniques, max-length, indexes)"
```

---

## Task 7.5: InteractiveCity + Notifications + Surveys configurations (8 entities)

**Files:**
- Create: `backend/src/CCE.Infrastructure/Persistence/Configurations/InteractiveCity/{CityScenarioConfiguration,CityTechnologyConfiguration,CityScenarioResultConfiguration}.cs`
- Create: `backend/src/CCE.Infrastructure/Persistence/Configurations/Notifications/{NotificationTemplateConfiguration,UserNotificationConfiguration}.cs`
- Create: `backend/src/CCE.Infrastructure/Persistence/Configurations/Surveys/{ServiceRatingConfiguration,SearchQueryLogConfiguration}.cs`

- [ ] **Step 1: InteractiveCity**

`InteractiveCity/CityScenarioConfiguration.cs`:

```csharp
using CCE.Domain.InteractiveCity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.InteractiveCity;

internal sealed class CityScenarioConfiguration : IEntityTypeConfiguration<CityScenario>
{
    public void Configure(EntityTypeBuilder<CityScenario> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.NameAr).HasMaxLength(256).IsRequired();
        builder.Property(s => s.NameEn).HasMaxLength(256).IsRequired();
        builder.Property(s => s.CityType).HasConversion<int>();
        builder.Property(s => s.ConfigurationJson).HasColumnType("nvarchar(max)");
        builder.HasIndex(s => new { s.UserId, s.LastModifiedOn }).HasDatabaseName("ix_city_scenario_user_modified");
        builder.Ignore(s => s.DomainEvents);
    }
}
```

`InteractiveCity/CityTechnologyConfiguration.cs`:

```csharp
using CCE.Domain.InteractiveCity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.InteractiveCity;

internal sealed class CityTechnologyConfiguration : IEntityTypeConfiguration<CityTechnology>
{
    public void Configure(EntityTypeBuilder<CityTechnology> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.NameAr).HasMaxLength(256).IsRequired();
        builder.Property(t => t.NameEn).HasMaxLength(256).IsRequired();
        builder.Property(t => t.CategoryAr).HasMaxLength(128).IsRequired();
        builder.Property(t => t.CategoryEn).HasMaxLength(128).IsRequired();
        builder.Property(t => t.IconUrl).HasMaxLength(2048);
        builder.Property(t => t.CarbonImpactKgPerYear).HasPrecision(18, 2);
        builder.Property(t => t.CostUsd).HasPrecision(18, 2);
        builder.HasIndex(t => t.IsActive).HasDatabaseName("ix_city_tech_is_active");
    }
}
```

`InteractiveCity/CityScenarioResultConfiguration.cs`:

```csharp
using CCE.Domain.InteractiveCity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.InteractiveCity;

internal sealed class CityScenarioResultConfiguration : IEntityTypeConfiguration<CityScenarioResult>
{
    public void Configure(EntityTypeBuilder<CityScenarioResult> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.ComputedTotalCostUsd).HasPrecision(18, 2);
        builder.Property(r => r.EngineVersion).HasMaxLength(64).IsRequired();
        builder.HasIndex(r => new { r.ScenarioId, r.ComputedAt }).HasDatabaseName("ix_city_result_scenario_at");
    }
}
```

- [ ] **Step 2: Notifications**

`Notifications/NotificationTemplateConfiguration.cs`:

```csharp
using CCE.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Notifications;

internal sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.Code).HasMaxLength(64).IsRequired();
        builder.Property(t => t.SubjectAr).HasMaxLength(512).IsRequired();
        builder.Property(t => t.SubjectEn).HasMaxLength(512).IsRequired();
        builder.Property(t => t.BodyAr).HasColumnType("nvarchar(max)");
        builder.Property(t => t.BodyEn).HasColumnType("nvarchar(max)");
        builder.Property(t => t.Channel).HasConversion<int>();
        builder.Property(t => t.VariableSchemaJson).HasColumnType("nvarchar(max)");
        builder.HasIndex(t => t.Code).IsUnique().HasDatabaseName("ux_notification_template_code");
    }
}
```

`Notifications/UserNotificationConfiguration.cs`:

```csharp
using CCE.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Notifications;

internal sealed class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(EntityTypeBuilder<UserNotification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();
        builder.Property(n => n.RenderedSubjectAr).HasMaxLength(512);
        builder.Property(n => n.RenderedSubjectEn).HasMaxLength(512);
        builder.Property(n => n.RenderedBody).HasColumnType("nvarchar(max)");
        builder.Property(n => n.RenderedLocale).HasMaxLength(2).IsRequired();
        builder.Property(n => n.Channel).HasConversion<int>();
        builder.Property(n => n.Status).HasConversion<int>();
        builder.HasIndex(n => new { n.UserId, n.Status }).HasDatabaseName("ix_user_notification_user_status");
    }
}
```

- [ ] **Step 3: Surveys**

`Surveys/ServiceRatingConfiguration.cs`:

```csharp
using CCE.Domain.Surveys;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Surveys;

internal sealed class ServiceRatingConfiguration : IEntityTypeConfiguration<ServiceRating>
{
    public void Configure(EntityTypeBuilder<ServiceRating> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Page).HasMaxLength(256).IsRequired();
        builder.Property(r => r.Locale).HasMaxLength(2).IsRequired();
        builder.Property(r => r.CommentAr).HasMaxLength(2000);
        builder.Property(r => r.CommentEn).HasMaxLength(2000);
        builder.HasIndex(r => r.SubmittedOn).HasDatabaseName("ix_service_rating_submitted_on");
    }
}
```

`Surveys/SearchQueryLogConfiguration.cs`:

```csharp
using CCE.Domain.Surveys;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CCE.Infrastructure.Persistence.Configurations.Surveys;

internal sealed class SearchQueryLogConfiguration : IEntityTypeConfiguration<SearchQueryLog>
{
    public void Configure(EntityTypeBuilder<SearchQueryLog> builder)
    {
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Id).ValueGeneratedNever();
        builder.Property(q => q.QueryText).HasMaxLength(1000).IsRequired();
        builder.Property(q => q.Locale).HasMaxLength(2).IsRequired();
        builder.HasIndex(q => q.SubmittedOn).HasDatabaseName("ix_search_query_log_submitted_on");
    }
}
```

- [ ] **Step 4: Build + commit**

```bash
dotnet build backend/src/CCE.Infrastructure/CCE.Infrastructure.csproj --nologo --no-restore 2>&1 | tail -5
git add backend/src/CCE.Infrastructure/Persistence/Configurations/InteractiveCity/ backend/src/CCE.Infrastructure/Persistence/Configurations/Notifications/ backend/src/CCE.Infrastructure/Persistence/Configurations/Surveys/
git -c commit.gpgsign=false commit -m "feat(persistence): EF configurations for InteractiveCity + Notifications + Surveys (8 entities)"
```

---

## Task 7.6: `ICurrentUserAccessor` + `AuditingInterceptor`

**Files:**
- Create: `backend/src/CCE.Application/Common/Interfaces/ICurrentUserAccessor.cs`
- Create: `backend/src/CCE.Infrastructure/Persistence/Interceptors/AuditingInterceptor.cs`
- Create: `backend/tests/CCE.Infrastructure.Tests/Persistence/AuditingInterceptorTests.cs`

- [ ] **Step 1: `ICurrentUserAccessor` (in Application — pattern matches Foundation)**

```csharp
namespace CCE.Application.Common.Interfaces;

/// <summary>
/// Provides the actor identifier of the current request for use by audit / domain logic.
/// Returns <c>"system"</c> for background jobs and seeders. Implementations live in:
/// - <c>CCE.Api.Internal</c> / <c>CCE.Api.External</c> — HttpContext-based.
/// - <c>CCE.Infrastructure.Tests</c> — fake.
/// - Seeder CLI — fixed <c>"seeder"</c>.
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>
    /// Stable, audit-friendly actor string. Common values:
    /// <c>"user:{guid}"</c>, <c>"upn:{email}"</c>, <c>"system"</c>, <c>"seeder"</c>.
    /// </summary>
    string GetActor();

    /// <summary>Optional correlation id (e.g., trace id). Returns <see cref="System.Guid.Empty"/> when none.</summary>
    System.Guid GetCorrelationId();
}
```

- [ ] **Step 2: `AuditingInterceptor`**

```csharp
using System.Text.Json;
using CCE.Application.Common.Interfaces;
using CCE.Domain.Audit;
using CCE.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CCE.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Spec §5.4. For every <see cref="AuditedAttribute"/>-marked entity entering the
/// <see cref="DbContext"/>'s ChangeTracker in Added/Modified/Deleted state, this interceptor
/// inserts an <see cref="AuditEvent"/> in the same transaction. Diff JSON captures the
/// minimal property delta (full body for Added/Deleted, only changed properties for Modified).
/// </summary>
public sealed class AuditingInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly ICurrentUserAccessor _userAccessor;
    private readonly ISystemClock _clock;

    public AuditingInterceptor(ICurrentUserAccessor userAccessor, ISystemClock clock)
    {
        _userAccessor = userAccessor;
        _clock = clock;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var ctx = eventData.Context;
        if (ctx is null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var actor = _userAccessor.GetActor();
        var correlationId = _userAccessor.GetCorrelationId();
        var now = _clock.UtcNow;

        var auditEvents = new List<AuditEvent>();
        foreach (var entry in ctx.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }
            var entityType = entry.Entity.GetType();
            if (!IsAudited(entityType)) continue;

            var entityName = entityType.Name;
            var resourceId = TryGetEntityId(entry);
            var diff = BuildDiff(entry);
            var action = $"{entityName}.{entry.State}";
            var resource = resourceId is null
                ? $"{entityName}/?"
                : $"{entityName}/{resourceId}";

            auditEvents.Add(new AuditEvent(
                id: System.Guid.NewGuid(),
                occurredOn: now,
                actor: actor,
                action: action,
                resource: resource,
                correlationId: correlationId,
                diff: diff));
        }

        if (auditEvents.Count > 0)
        {
            ctx.AddRange(auditEvents);
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static bool IsAudited(System.Type type)
        => type.GetCustomAttributes(typeof(AuditedAttribute), inherit: true).Length > 0;

    private static object? TryGetEntityId(EntityEntry entry)
    {
        var idProp = entry.Metadata.FindPrimaryKey()?.Properties[0];
        if (idProp is null) return null;
        return entry.Property(idProp.Name).CurrentValue;
    }

    private static string BuildDiff(EntityEntry entry)
    {
        switch (entry.State)
        {
            case EntityState.Added:
                {
                    var dict = new Dictionary<string, object?>();
                    foreach (var p in entry.Properties)
                    {
                        dict[p.Metadata.Name] = p.CurrentValue;
                    }
                    return JsonSerializer.Serialize(dict, JsonOptions);
                }
            case EntityState.Deleted:
                {
                    var dict = new Dictionary<string, object?>();
                    foreach (var p in entry.Properties)
                    {
                        dict[p.Metadata.Name] = p.OriginalValue;
                    }
                    return JsonSerializer.Serialize(dict, JsonOptions);
                }
            case EntityState.Modified:
                {
                    var dict = new Dictionary<string, object?>();
                    foreach (var p in entry.Properties)
                    {
                        if (!p.IsModified) continue;
                        dict[p.Metadata.Name] = new { Old = p.OriginalValue, New = p.CurrentValue };
                    }
                    return JsonSerializer.Serialize(dict, JsonOptions);
                }
            default:
                return "{}";
        }
    }
}
```

- [ ] **Step 3: Tests** (uses EF InMemory provider — fast, no DB)

```csharp
using CCE.Application.Common.Interfaces;
using CCE.Domain.Audit;
using CCE.Domain.Common;
using CCE.Domain.Country;
using CCE.Infrastructure.Persistence;
using CCE.Infrastructure.Persistence.Interceptors;
using CCE.TestInfrastructure.Time;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace CCE.Infrastructure.Tests.Persistence;

public class AuditingInterceptorTests
{
    private static (CceDbContext Ctx, FakeSystemClock Clock, ICurrentUserAccessor Accessor) Build()
    {
        var clock = new FakeSystemClock();
        var accessor = Substitute.For<ICurrentUserAccessor>();
        accessor.GetActor().Returns("user:test");
        accessor.GetCorrelationId().Returns(System.Guid.NewGuid());
        var interceptor = new AuditingInterceptor(accessor, clock);
        var options = new DbContextOptionsBuilder<CceDbContext>()
            .UseInMemoryDatabase(System.Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;
        var ctx = new CceDbContext(options);
        return (ctx, clock, accessor);
    }

    [Fact]
    public async Task Saving_audited_entity_writes_an_AuditEvent_in_same_transaction()
    {
        var (ctx, clock, _) = Build();
        var country = CCE.Domain.Country.Country.Register(
            "SAU", "SA", "السعودية", "Saudi Arabia", "آسيا", "Asia",
            "https://flags.example/sa.svg");
        ctx.Countries.Add(country);

        await ctx.SaveChangesAsync();

        var events = ctx.AuditEvents.AsNoTracking().ToList();
        events.Should().HaveCount(1);
        events[0].Actor.Should().Be("user:test");
        events[0].Action.Should().Be("Country.Added");
        events[0].Resource.Should().Be($"Country/{country.Id}");
        events[0].OccurredOn.Should().Be(clock.UtcNow);
    }

    [Fact]
    public async Task Modifying_entity_diffs_changed_properties_only()
    {
        var (ctx, _, _) = Build();
        var country = CCE.Domain.Country.Country.Register(
            "SAU", "SA", "السعودية", "Saudi Arabia", "آسيا", "Asia",
            "https://flags.example/sa.svg");
        ctx.Countries.Add(country);
        await ctx.SaveChangesAsync();

        // Clear add-event so the next save's audit count is observable.
        ctx.AuditEvents.RemoveRange(ctx.AuditEvents);
        await ctx.SaveChangesAsync();

        country.Deactivate();
        await ctx.SaveChangesAsync();

        var modifyEvents = ctx.AuditEvents.AsNoTracking()
            .Where(e => e.Action == "Country.Modified").ToList();
        modifyEvents.Should().HaveCount(1);
        modifyEvents[0].Diff.Should().Contain("IsActive");
        modifyEvents[0].Diff.Should().NotContain("\"NameAr\""); // unchanged
    }

    [Fact]
    public async Task Saving_a_non_audited_entity_writes_no_AuditEvent()
    {
        var (ctx, _, _) = Build();
        // CityScenarioResult is intentionally not audited (high-volume).
        var result = CCE.Domain.InteractiveCity.CityScenarioResult.Compute(
            System.Guid.NewGuid(), 2050, 100m, "v1", new FakeSystemClock());
        ctx.CityScenarioResults.Add(result);

        await ctx.SaveChangesAsync();

        ctx.AuditEvents.Should().BeEmpty();
    }
}
```

- [ ] **Step 4: Build + run + commit**

```bash
dotnet build backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj --nologo --no-restore 2>&1 | tail -5
dotnet test backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj --nologo --no-build --no-restore --filter "FullyQualifiedName~AuditingInterceptorTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
```

Expected: `Passed:     3`.

If `Microsoft.EntityFrameworkCore.InMemory` isn't referenced from `CCE.Infrastructure.Tests`, add it to the test csproj's PackageReferences AND to CPM with version `8.0.10`.

```bash
git add backend/src/CCE.Application/Common/Interfaces/ICurrentUserAccessor.cs backend/src/CCE.Infrastructure/Persistence/Interceptors/AuditingInterceptor.cs backend/tests/CCE.Infrastructure.Tests/Persistence/AuditingInterceptorTests.cs
git -c commit.gpgsign=false commit -m "feat(persistence): AuditingInterceptor scanning [Audited] entities + ICurrentUserAccessor (3 TDD tests)"
```

---

## Task 7.7: `DomainEventDispatcher`

**Files:**
- Create: `backend/src/CCE.Infrastructure/Persistence/Interceptors/DomainEventDispatcher.cs`
- Create: `backend/tests/CCE.Infrastructure.Tests/Persistence/DomainEventDispatcherTests.cs`

- [ ] **Step 1: Dispatcher**

```csharp
using CCE.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CCE.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Spec §3.5 + §5.4. Post-commit interceptor that drains <see cref="IDomainEvent"/>s from
/// every aggregate root tracked by the context and publishes them via <see cref="IPublisher"/>
/// (MediatR). In-process synchronous handlers only (sub-project 2 requirement). Outbox is
/// sub-project 8 work.
/// </summary>
public sealed class DomainEventDispatcher : SaveChangesInterceptor
{
    private readonly IPublisher _publisher;

    public DomainEventDispatcher(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        var ctx = eventData.Context;
        if (ctx is null) return await base.SavedChangesAsync(eventData, result, cancellationToken);

        var entriesWithEvents = ctx.ChangeTracker.Entries()
            .Where(e => e.Entity.GetType().IsSubclassOf(typeof(AggregateRoot<System.Guid>))
                        || e.Entity is Entity<System.Guid> { DomainEvents.Count: > 0 })
            .Select(e => e.Entity)
            .OfType<Entity<System.Guid>>()
            .Where(entity => entity.DomainEvents.Count > 0)
            .ToList();

        var allEvents = entriesWithEvents.SelectMany(e => e.DomainEvents).ToList();

        // Clear before publishing so reentrant SaveChanges doesn't republish.
        foreach (var entity in entriesWithEvents)
        {
            entity.ClearDomainEvents();
        }

        foreach (var domainEvent in allEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken).ConfigureAwait(false);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
```

- [ ] **Step 2: Tests**

```csharp
using CCE.Domain.Identity;
using CCE.Domain.Identity.Events;
using CCE.Infrastructure.Persistence;
using CCE.Infrastructure.Persistence.Interceptors;
using CCE.TestInfrastructure.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace CCE.Infrastructure.Tests.Persistence;

public class DomainEventDispatcherTests
{
    private static (CceDbContext Ctx, IPublisher Publisher) Build()
    {
        var publisher = Substitute.For<IPublisher>();
        var dispatcher = new DomainEventDispatcher(publisher);
        var options = new DbContextOptionsBuilder<CceDbContext>()
            .UseInMemoryDatabase(System.Guid.NewGuid().ToString())
            .AddInterceptors(dispatcher)
            .Options;
        return (new CceDbContext(options), publisher);
    }

    [Fact]
    public async Task Saved_aggregate_with_event_publishes_it()
    {
        var (ctx, publisher) = Build();
        var clock = new FakeSystemClock();
        var req = ExpertRegistrationRequest.Submit(
            System.Guid.NewGuid(), "خبير", "Expert", new[] { "Solar" }, clock);
        req.Approve(System.Guid.NewGuid(), clock);

        ctx.ExpertRegistrationRequests.Add(req);
        await ctx.SaveChangesAsync();

        await publisher.Received(1).Publish(
            Arg.Any<ExpertRegistrationApprovedEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DomainEvents_cleared_after_publish()
    {
        var (ctx, _) = Build();
        var clock = new FakeSystemClock();
        var req = ExpertRegistrationRequest.Submit(
            System.Guid.NewGuid(), "خبير", "Expert", new[] { "Solar" }, clock);
        req.Approve(System.Guid.NewGuid(), clock);
        ctx.ExpertRegistrationRequests.Add(req);

        await ctx.SaveChangesAsync();

        req.DomainEvents.Should().BeEmpty();
    }
}
```

- [ ] **Step 3: Build + run + commit**

```bash
dotnet test backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~DomainEventDispatcherTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
git add backend/src/CCE.Infrastructure/Persistence/Interceptors/DomainEventDispatcher.cs backend/tests/CCE.Infrastructure.Tests/Persistence/DomainEventDispatcherTests.cs
git -c commit.gpgsign=false commit -m "feat(persistence): DomainEventDispatcher draining + publishing domain events post-commit (2 TDD tests)"
```

---

## Task 7.8: `DbExceptionMapper`

**Files:**
- Create: `backend/src/CCE.Infrastructure/Persistence/DbExceptionMapper.cs`
- Create: `backend/src/CCE.Domain/Common/DuplicateException.cs`
- Create: `backend/src/CCE.Domain/Common/ConcurrencyException.cs`
- Create: `backend/tests/CCE.Infrastructure.Tests/Persistence/DbExceptionMapperTests.cs`

- [ ] **Step 1: Two new exception types**

`Common/DuplicateException.cs`:

```csharp
namespace CCE.Domain.Common;

/// <summary>Raised when a unique-index violation indicates a duplicate value.</summary>
public sealed class DuplicateException : DomainException
{
    public DuplicateException() { }
    public DuplicateException(string message) : base(message) { }
    public DuplicateException(string message, System.Exception innerException) : base(message, innerException) { }
}
```

`Common/ConcurrencyException.cs`:

```csharp
namespace CCE.Domain.Common;

/// <summary>Raised when an optimistic-concurrency token mismatch occurs.</summary>
public sealed class ConcurrencyException : DomainException
{
    public ConcurrencyException() { }
    public ConcurrencyException(string message) : base(message) { }
    public ConcurrencyException(string message, System.Exception innerException) : base(message, innerException) { }
}
```

- [ ] **Step 2: Mapper**

```csharp
using CCE.Domain.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Persistence;

/// <summary>
/// Maps SQL Server exceptions surfaced by EF Core into domain exceptions, so the
/// application/UI layer doesn't need to know SQL error numbers.
/// Recognized:
/// - 2601 / 2627 → <see cref="DuplicateException"/>
/// - <see cref="DbUpdateConcurrencyException"/> → <see cref="ConcurrencyException"/>
/// - everything else → rethrown unchanged.
/// </summary>
public static class DbExceptionMapper
{
    public const int SqlUniqueConstraintViolation = 2627;
    public const int SqlUniqueIndexViolation = 2601;

    public static System.Exception Map(System.Exception ex)
    {
        if (ex is DbUpdateConcurrencyException concurrency)
        {
            return new ConcurrencyException("Concurrent update conflict.", concurrency);
        }
        if (ex is DbUpdateException dbUpdate
            && dbUpdate.InnerException is SqlException sqlInner
            && (sqlInner.Number == SqlUniqueConstraintViolation
                || sqlInner.Number == SqlUniqueIndexViolation))
        {
            return new DuplicateException("Duplicate value rejected by unique index.", dbUpdate);
        }
        return ex;
    }
}
```

- [ ] **Step 3: Tests**

```csharp
using CCE.Domain.Common;
using CCE.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CCE.Infrastructure.Tests.Persistence;

public class DbExceptionMapperTests
{
    [Fact]
    public void DbUpdateConcurrencyException_maps_to_ConcurrencyException()
    {
        var ex = new DbUpdateConcurrencyException("test");
        var mapped = DbExceptionMapper.Map(ex);
        mapped.Should().BeOfType<ConcurrencyException>();
        mapped.InnerException.Should().BeSameAs(ex);
    }

    [Fact]
    public void Unknown_exception_passes_through()
    {
        var ex = new System.InvalidOperationException("not a db error");
        var mapped = DbExceptionMapper.Map(ex);
        mapped.Should().BeSameAs(ex);
    }

    // SqlException can't be constructed directly; the duplicate-mapping path is
    // exercised end-to-end in Phase 08 integration tests against a real DB.
}
```

- [ ] **Step 4: Build + run + commit**

```bash
dotnet test backend/tests/CCE.Infrastructure.Tests/CCE.Infrastructure.Tests.csproj --nologo --no-restore --filter "FullyQualifiedName~DbExceptionMapperTests" --logger "console;verbosity=minimal" 2>&1 | tail -3
git add backend/src/CCE.Domain/Common/DuplicateException.cs backend/src/CCE.Domain/Common/ConcurrencyException.cs backend/src/CCE.Infrastructure/Persistence/DbExceptionMapper.cs backend/tests/CCE.Infrastructure.Tests/Persistence/DbExceptionMapperTests.cs
git -c commit.gpgsign=false commit -m "feat(persistence): DbExceptionMapper (Sql 2601/2627 → Duplicate, ConcurrencyException) (2 TDD tests)"
```

---

## Task 7.9: DI wiring + phase close

**Files:**
- Modify: `backend/src/CCE.Infrastructure/DependencyInjection.cs` (register interceptors + ICurrentUserAccessor)
- Modify: `docs/subprojects/02-data-domain-progress.md` (mark Phase 07 done)

- [ ] **Step 1: Register interceptors in DI**

Open `backend/src/CCE.Infrastructure/DependencyInjection.cs`. Find the `AddDbContext<CceDbContext>` call (or wherever the DbContextOptions are configured) and ensure it now adds the two interceptors. Concrete edit depends on the existing wiring; the shape:

```csharp
services.AddScoped<AuditingInterceptor>();
services.AddScoped<DomainEventDispatcher>();

services.AddDbContext<CceDbContext>((sp, options) =>
{
    options.UseSqlServer(/* existing */);
    options.UseSnakeCaseNamingConvention();
    options.AddInterceptors(
        sp.GetRequiredService<AuditingInterceptor>(),
        sp.GetRequiredService<DomainEventDispatcher>());
});

services.AddScoped<ICceDbContext>(sp => sp.GetRequiredService<CceDbContext>());
```

The `ICurrentUserAccessor` implementation lives in the API host projects (sub-project 3), so for now register a `SystemCurrentUserAccessor` fallback in Infrastructure DI:

```csharp
internal sealed class SystemCurrentUserAccessor : ICurrentUserAccessor
{
    public string GetActor() => "system";
    public System.Guid GetCorrelationId() => System.Guid.Empty;
}

services.TryAddScoped<ICurrentUserAccessor, SystemCurrentUserAccessor>();
```

(API hosts later override this with an HttpContext-based implementation.)

- [ ] **Step 2: Full backend build + test**

```bash
dotnet build backend/CCE.sln --nologo --no-restore 2>&1 | tail -8
dotnet test backend/CCE.sln --nologo --no-restore --logger "console;verbosity=minimal" 2>&1 | grep -E "(Passed!|Failed!)"
```

Expected: 0 errors, all 5 test projects green. Domain.Tests count unchanged (~284). Infrastructure.Tests count grew by ~7 (3 audit + 2 dispatcher + 2 mapper).

- [ ] **Step 3: Update progress doc**

Mark Phase 07 ✅ Done. Use the actual numbers reported.

- [ ] **Step 4: Commit**

```bash
git add backend/src/CCE.Infrastructure/DependencyInjection.cs docs/subprojects/02-data-domain-progress.md
git -c commit.gpgsign=false commit -m "feat(persistence): wire interceptors + ICurrentUserAccessor in DI; Phase 07 close"
```

---

## Phase 07 — completion checklist

- [ ] `CceDbContext` extends `IdentityDbContext<User, Role, Guid>` with 30+ DbSets.
- [ ] Soft-delete query filter registered for every `ISoftDeletable` entity via reflection.
- [ ] `IEntityTypeConfiguration<T>` for every entity that needs indexes / RowVersion / JSON columns.
- [ ] Unique indexes for `slug`, `iso_alpha3`, `email`, `code`, etc., filtered on `is_deleted=0`.
- [ ] `AuditingInterceptor` writes `AuditEvent` for every Added/Modified/Deleted `[Audited]` entity.
- [ ] `DomainEventDispatcher` publishes domain events post-commit via MediatR.
- [ ] `DbExceptionMapper` translates SQL unique-violation + DbUpdateConcurrencyException.
- [ ] `ICurrentUserAccessor` interface defined; `SystemCurrentUserAccessor` fallback wired.
- [ ] All Phase 06 regression tests still pass.
- [ ] 9 new commits.

**If all boxes ticked, Phase 07 is complete. Proceed to Phase 08 (DataDomainInitial migration + index plan).**
