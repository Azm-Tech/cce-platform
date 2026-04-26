# CCE Sub-Project 02 — Data & Domain — Design Spec

**Project:** Circular Carbon Economy (CCE) Knowledge Center Platform — Phase 2
**Client:** Saudi Ministry of Energy — Sustainability & Climate Change Agency
**Sub-project:** 02 — Data & Domain
**Spec owner:** CCE build team
**Date:** 2026-04-27
**Status:** Draft — awaiting user review
**Predecessor:** [Foundation v0.1.0](../../foundation-completion.md)
**Brief:** [`docs/subprojects/02-data-domain.md`](../../subprojects/02-data-domain.md)

---

## 1. Purpose

Define the full Entity Framework Core schema for the CCE Knowledge Center, ship initial migrations + seed data, expand `permissions.yaml` to the full BRD §4.1.31 matrix, and give the Domain layer everything sub-projects 3 (Internal API), 4 (External API), 5 (Admin CMS), 6 (Web Portal), and 7 (Feature modules) need to build against.

After sub-project 2:

- The database is reproducibly creatable from migrations.
- The permissions catalog is the single source of truth for authorization.
- ~36 entities across 8 bounded contexts are persisted, soft-deletable, and audited.
- Schema parity is verified between Azure SQL Edge (dev arm64) and SQL Server 2022 (prod amd64).
- Domain unit-test coverage ≥ 90%.

This sub-project ships **no API endpoints, no UI, no integrations.** Those are sub-projects 3+. Sub-project 2 adds: entities, value objects, domain methods, EF mappings, migrations, the SaveChangesInterceptor for auditing, the seeder, and ~265 new tests.

---

## 2. Locked decisions (from brainstorming)

| # | Decision |
|---|---|
| D2-1 | All ~36 entities ship in this sub-project (big-bang) — not vertical slices |
| D2-2 | Bilingual content uses inline `*_ar` + `*_en` columns; not translation tables, not JSON |
| D2-3 | Single `CceDbContext : IdentityDbContext<User, Role, Guid>` — Identity tables + CCE entities in one context |
| D2-4 | Soft delete via `IsDeleted` + `DeletedOn` + `DeletedById` + EF global query filter |
| D2-5 | Plain `Guid` IDs everywhere — no strongly-typed ID wrappers |
| D2-6 | Audit via `SaveChangesInterceptor` + `[Audited]` attribute on entities (no manual audit calls) |
| D2-7 | `permissions.yaml` uses nested groups + per-permission metadata + role-default mappings |
| D2-8 | KAPSARC data: snapshot table per country, not live-only |

These decisions become ADR-0019 through ADR-0026 at sub-project 2 close.

---

## 3. Architecture

### 3.1 Bounded contexts inside `CCE.Domain`

```
CCE.Domain/
├── Common/          (Foundation: Entity, AggregateRoot, ValueObject, IDomainEvent, ISystemClock)
├── Audit/           (Foundation: AuditEvent)
├── Identity/        User, Role, StateRepresentativeAssignment, ExpertProfile, ExpertRegistrationRequest
├── Content/         Resource, ResourceCategory, News, Event, Page, HomepageSection, NewsletterSubscription, AssetFile
├── Country/         Country, CountryProfile, CountryResourceRequest, CountryKapsarcSnapshot
├── Community/       Topic, Post, PostReply, PostRating, TopicFollow, UserFollow, PostFollow
├── KnowledgeMaps/   KnowledgeMap, KnowledgeMapNode, KnowledgeMapEdge, KnowledgeMapAssociation
├── InteractiveCity/ CityScenario, CityTechnology, CityScenarioResult
├── Notifications/   NotificationTemplate, UserNotification
└── Surveys/         ServiceRating, SearchQueryLog
```

**Bounded contexts share one DbContext.** The namespace split is for code-org clarity, not transactional/storage separation.

### 3.2 Persistence layer (`CCE.Infrastructure`)

```
CCE.Infrastructure/
├── Persistence/
│   ├── CceDbContext.cs                  : IdentityDbContext<User, Role, Guid>
│   ├── CceDbContextDesignTimeFactory.cs (existing)
│   ├── Configurations/                  IEntityTypeConfiguration<T> per entity
│   │   ├── Identity/
│   │   ├── Content/
│   │   ├── Country/
│   │   ├── Community/
│   │   ├── KnowledgeMaps/
│   │   ├── InteractiveCity/
│   │   ├── Notifications/
│   │   ├── Surveys/
│   │   └── AuditEventConfiguration.cs   (existing)
│   ├── Interceptors/
│   │   ├── AuditingInterceptor.cs       (NEW: SaveChangesInterceptor scanning [Audited])
│   │   └── DomainEventDispatcher.cs     (NEW: post-commit MediatR publish)
│   ├── DbExceptionMapper.cs             (NEW: SQL error → domain exception)
│   ├── Migrations/
│   │   ├── 20260425..._InitialAuditEvents (existing)
│   │   ├── 20260425..._AuditEventsAppendOnlyTrigger (existing)
│   │   └── 20260427..._DataDomainInitial (NEW: ~36 tables + indexes + FKs)
│   └── Seed/
│       ├── SeedRunner.cs                (NEW: idempotent seeder, CLI entry)
│       ├── RolesAndPermissionsSeeder.cs
│       ├── ReferenceDataSeeder.cs       (countries, technologies, templates)
│       └── DemoDataSeeder.cs            (--demo flag only)
└── (existing services unchanged)
```

### 3.3 Permissions source generator (extends Phase 04)

`CCE.Domain.SourceGenerators` already emits `Permissions.System_Health_Read` from a flat YAML list. Sub-project 2 expands it:

- **YAML schema** — nested `groups`, per-permission `description` + `roles`.
- **Generator output** — same `Permissions.<Name>` constants + a new `RolePermissionMap` static class with one collection per role.
- **Backward compat** — Foundation's flat-format YAML still parses (the generator detects schema and adapts).

### 3.4 Migrations strategy

Sub-project 2 ships **one consolidated migration** (`DataDomainInitial`) that:

1. Creates all ASP.NET Identity tables (auto-emitted by `IdentityDbContext`).
2. Creates all ~36 CCE entity tables.
3. Creates indexes (covered in §5.2).
4. Creates SQL full-text catalogs + indexes on bilingual content fields.
5. Adds rowversion columns on entities that need optimistic concurrency.

Foundation's two existing migrations (`InitialAuditEvents` + `AuditEventsAppendOnlyTrigger`) **stay**. They're already applied to dev DBs; squashing would force a re-deploy. Sub-project 2's migration runs after them in `__EFMigrationsHistory` order.

### 3.5 Domain events

- Entities raise `IDomainEvent` instances during behavior (already supported by `Entity<TId>` from Foundation).
- `CceDbContext.SaveChangesAsync` collects events from tracked entities, clears them after successful commit, and publishes via `IPublisher` (MediatR).
- **In-process synchronous handlers only** for sub-project 2 (search-index updater, notification fan-out lookup). Outbox + external dispatch lands in sub-project 8.

---

## 4. Components — entity catalog

### 4.1 Identity (5 entities)

| Entity | Inherits | Key columns | Notable invariants |
|---|---|---|---|
| `User` | `IdentityUser<Guid>` | + `LocalePreference` (ar/en), `KnowledgeLevel` (enum: Beginner/Intermediate/Advanced), `Interests` (JSON of string array), `CountryId?`, `AvatarUrl?` | Email unique (Identity); LocalePreference defaults to "ar"; KnowledgeLevel defaults to Beginner |
| `Role` | `IdentityRole<Guid>` | normalized name | Roles seeded from `permissions.yaml` defaults at startup |
| `StateRepresentativeAssignment` | `Entity<Guid>` | `UserId`, `CountryId`, `AssignedOn`, `AssignedById`, `RevokedOn?`, `RevokedById?`, `IsDeleted`* | Unique active assignment per (UserId, CountryId); soft-delete used for revocation |
| `ExpertProfile` | `Entity<Guid>` | `UserId` (1:1), `BioAr`, `BioEn`, `ExpertiseTags` (JSON), `AcademicTitleAr/En`, `ApprovedOn`, `ApprovedById`, `IsDeleted`* | Created only after admin approves an `ExpertRegistrationRequest` |
| `ExpertRegistrationRequest` | `AggregateRoot<Guid>` | `RequestedById`, `Status` (Pending/Approved/Rejected), `RequestedBioAr/En`, `RequestedTags`, `ProcessedById?`, `ProcessedOn?`, `RejectionReasonAr/En?`, `IsDeleted`* | Status state machine: Pending → Approved or Rejected (terminal); approving creates `ExpertProfile` |

\*soft-deletable

### 4.2 Content (8 entities)

| Entity | Inherits | Key columns | Notable |
|---|---|---|---|
| `Resource` | `AggregateRoot<Guid>` | `TitleAr/En`, `DescriptionAr/En`, `ResourceType` enum (PDF/Video/Image/Link/Document), `CategoryId`, `CountryId?` (null = center), `UploadedById`, `AssetFileId`, `PublishedOn`, `ViewCount`, `IsDeleted`*, `RowVersion` | Discriminator: Center vs Country resource via `CountryId` nullability; `[Audited]` |
| `ResourceCategory` | `Entity<Guid>` | `NameAr/En`, `Slug`, `ParentId?`, `OrderIndex`, `IsActive` | Tree (hierarchical via ParentId); soft-deletable indirectly via IsActive |
| `News` | `AggregateRoot<Guid>` | `TitleAr/En`, `ContentAr/En` (rich text), `Slug`, `FeaturedImageUrl?`, `PublishedOn?`, `AuthorId`, `IsFeatured`, `IsDeleted`*, `RowVersion` | Slug unique; published when `PublishedOn != null`; `[Audited]` |
| `Event` | `AggregateRoot<Guid>` | `TitleAr/En`, `DescriptionAr/En`, `StartsOn`, `EndsOn`, `LocationAr/En?`, `OnlineMeetingUrl?`, `FeaturedImageUrl?`, `ICalUid` (stable for .ics regeneration), `IsDeleted`*, `RowVersion` | `EndsOn > StartsOn`; ICalUid generated once at creation; `[Audited]` |
| `Page` | `AggregateRoot<Guid>` | `Slug`, `TitleAr/En`, `ContentAr/En`, `PageType` enum (AboutPlatform/TermsOfService/PrivacyPolicy/Custom), `IsDeleted`*, `RowVersion` | Slug unique within (PageType); `[Audited]` |
| `HomepageSection` | `Entity<Guid>` | `SectionType` enum, `OrderIndex`, `ContentAr/En` (JSON or rich-text), `IsActive`, `IsDeleted`* | Admin reorders via OrderIndex; `[Audited]` |
| `NewsletterSubscription` | `Entity<Guid>` | `Email`, `LocalePreference`, `IsConfirmed`, `ConfirmationToken`, `ConfirmedOn?`, `UnsubscribedOn?` | Email unique; double-opt-in via ConfirmationToken |
| `AssetFile` | `Entity<Guid>` | `Url` (or storage key), `OriginalFileName`, `SizeBytes`, `MimeType`, `UploadedById`, `UploadedOn`, `VirusScanStatus` enum (Pending/Clean/Infected/ScanFailed), `ScannedOn?` | Referenced by `Resource.AssetFileId`, `News.FeaturedImageUrl`, `Event.FeaturedImageUrl`; ClamAV result lives here |

### 4.3 Country (4 entities)

| Entity | Inherits | Key columns |
|---|---|---|
| `Country` | `AggregateRoot<Guid>` | `IsoAlpha3` (3-letter ISO code), `IsoAlpha2`, `NameAr/En`, `RegionAr/En`, `FlagUrl`, `LatestKapsarcSnapshotId?` (FK), `IsActive`, `IsDeleted`* |
| `CountryProfile` | `Entity<Guid>` | `CountryId` (1:1), `DescriptionAr/En`, `KeyInitiativesAr/En`, `ContactInfoAr/En?`, `LastUpdatedById`, `LastUpdatedOn`, `RowVersion` |
| `CountryResourceRequest` | `AggregateRoot<Guid>` | `CountryId`, `RequestedById`, `Status` enum (Pending/Approved/Rejected), `ProposedTitleAr/En`, `ProposedDescriptionAr/En`, `ProposedResourceType`, `ProposedAssetFileId`, `AdminNotesAr/En?`, `ProcessedById?`, `ProcessedOn?`, `IsDeleted`* |
| `CountryKapsarcSnapshot` | `Entity<Guid>` | `CountryId`, `Classification` (string/enum from KAPSARC vocabulary), `PerformanceScore` (decimal), `TotalIndex` (decimal), `SnapshotTakenOn`, `SourceVersion?` | Append-only by convention; latest pointer on Country |

### 4.4 Community (7 entities)

| Entity | Inherits | Key columns |
|---|---|---|
| `Topic` | `Entity<Guid>` | `NameAr/En`, `DescriptionAr/En`, `Slug`, `ParentId?`, `IconUrl?`, `OrderIndex`, `IsActive`, `IsDeleted`* |
| `Post` | `AggregateRoot<Guid>` | `TopicId`, `AuthorId`, `Content` (single language — author writes in their language), `Locale` ("ar" or "en"), `IsAnswerable` (question vs discussion), `AnsweredReplyId?`, `IsDeleted`* | `[Audited]`; Content max 8000 chars |
| `PostReply` | `Entity<Guid>` | `PostId`, `AuthorId`, `Content`, `Locale`, `ParentReplyId?` (threaded), `IsByExpert` (denormalized from `User.ExpertProfile != null` at creation time), `IsDeleted`* |
| `PostRating` | `Entity<Guid>` | `PostId`, `UserId`, `Stars` (1–5), `RatedOn` | Unique (PostId, UserId) |
| `TopicFollow` | `Entity<Guid>` | `TopicId`, `UserId`, `FollowedOn` | Unique (TopicId, UserId) |
| `UserFollow` | `Entity<Guid>` | `FollowerId`, `FollowedId`, `FollowedOn` | Unique (FollowerId, FollowedId); FollowerId ≠ FollowedId |
| `PostFollow` | `Entity<Guid>` | `PostId`, `UserId`, `FollowedOn` | Unique (PostId, UserId) |

### 4.5 Knowledge Maps (4 entities)

| Entity | Inherits | Key columns |
|---|---|---|
| `KnowledgeMap` | `AggregateRoot<Guid>` | `NameAr/En`, `DescriptionAr/En`, `Slug`, `IsActive`, `IsDeleted`*, `RowVersion` |
| `KnowledgeMapNode` | `Entity<Guid>` | `MapId`, `NameAr/En`, `NodeType` enum (Technology/Sector/SubTopic), `DescriptionAr/En?`, `IconUrl?`, `LayoutX`, `LayoutY`, `OrderIndex` |
| `KnowledgeMapEdge` | `Entity<Guid>` | `MapId`, `FromNodeId`, `ToNodeId`, `RelationshipType` enum (ParentOf/RelatedTo/RequiredBy), `OrderIndex` |
| `KnowledgeMapAssociation` | `Entity<Guid>` | `NodeId`, `AssociatedType` enum (Resource/News/Event), `AssociatedId` (Guid — polymorphic FK), `OrderIndex` |

### 4.6 Interactive City (3 entities)

| Entity | Inherits | Key columns |
|---|---|---|
| `CityScenario` | `AggregateRoot<Guid>` | `UserId`, `NameAr/En`, `CityType` enum (Coastal/Industrial/Mixed/Residential), `TargetYear`, `ConfigurationJson` (selected technologies + parameters), `CreatedOn`, `LastModifiedOn`, `IsDeleted`* |
| `CityTechnology` | `Entity<Guid>` | `NameAr/En`, `DescriptionAr/En`, `CategoryAr/En`, `CarbonImpactKgPerYear`, `CostUsd`, `IconUrl?`, `IsActive` |
| `CityScenarioResult` | `Entity<Guid>` | `ScenarioId`, `ComputedCarbonNeutralityYear?`, `ComputedTotalCostUsd`, `ComputedAt`, `EngineVersion` | Append-only; latest pointer on Scenario optional (denormalized for fast read) |

### 4.7 Notifications (2 entities)

| Entity | Inherits | Key columns |
|---|---|---|
| `NotificationTemplate` | `Entity<Guid>` | `Code` (e.g., `ACCOUNT_CREATED`), `SubjectAr/En`, `BodyAr/En`, `Channel` enum (Email/Sms/InApp), `VariableSchemaJson`, `IsActive` | Code unique; seeded from BRD §7.1 |
| `UserNotification` | `Entity<Guid>` | `UserId`, `TemplateId`, `RenderedSubjectAr/En`, `RenderedBody`, `RenderedLocale`, `Channel`, `SentOn`, `ReadOn?`, `Status` enum (Pending/Sent/Failed/Read) |

### 4.8 Surveys & Stats (2 entities)

| Entity | Inherits | Key columns |
|---|---|---|
| `ServiceRating` | `Entity<Guid>` | `UserId?` (nullable — anonymous OK), `Rating` (1–5), `CommentAr/En?`, `Page` (string — which page), `Locale`, `SubmittedOn` |
| `SearchQueryLog` | `Entity<Guid>` | `UserId?`, `QueryText`, `ResultsCount`, `ResponseTimeMs`, `Locale`, `SubmittedOn` |

### 4.9 Audit (1 existing)

`AuditEvent` (from Foundation Phase 06). Sub-project 2 doesn't change this entity. The new `AuditingInterceptor` writes rows here.

### 4.10 Aggregate roots (8 total)

`User`, `Resource`, `News`, `Event`, `Country`, `Post`, `KnowledgeMap`, `CityScenario`. Children are owned by their root; cascade soft-delete propagates from root.

### 4.11 [Audited] annotations

Apply `[Audited]` to: `User`, `Role`, `Resource`, `News`, `Event`, `Page`, `HomepageSection`, `Country`, `CountryProfile`, `CountryResourceRequest`, `Post`, `PostReply`, `Topic`, `KnowledgeMap`, `CityTechnology`, `NotificationTemplate`, `ExpertProfile`, `ExpertRegistrationRequest`, `StateRepresentativeAssignment`.

**Not audited** (high-volume or low-stakes): `PostRating`, `TopicFollow`, `UserFollow`, `PostFollow`, `UserNotification`, `ServiceRating`, `SearchQueryLog`, `CityScenarioResult`, `CountryKapsarcSnapshot`.

---

## 5. Persistence specifics

### 5.1 Concurrency control

Entities with multi-step admin workflows or high contention get `RowVersion` (`byte[]`):

`Resource`, `News`, `Event`, `Page`, `KnowledgeMap`, `CountryProfile`, `CountryResourceRequest`, `ExpertRegistrationRequest`.

EF Core 8 maps `RowVersion` to SQL Server `rowversion` automatically. Concurrency conflicts surface as `DbUpdateConcurrencyException` → 409 Conflict (handled in `DbExceptionMapper`).

### 5.2 Index plan

Each table gets:

- **Primary key** on `Id` (Guid, clustered = default).
- **Soft-delete filter index** — non-clustered on `(IsDeleted)` for tables with significant deleted-row volume (Posts, Resources).
- **FK indexes** on every foreign-key column (EF emits these automatically; we verify they're present).
- **Natural-key uniqueness** —
  - `Country.IsoAlpha3` UNIQUE
  - `Role.NormalizedName` UNIQUE (Identity)
  - `News.Slug` UNIQUE
  - `Page.(PageType, Slug)` UNIQUE
  - `NotificationTemplate.Code` UNIQUE
  - `Topic.Slug` UNIQUE
  - `KnowledgeMap.Slug` UNIQUE
  - `(PostId, UserId)` UNIQUE on PostRating, PostFollow
  - `(TopicId, UserId)` UNIQUE on TopicFollow
  - `(FollowerId, FollowedId)` UNIQUE on UserFollow
  - `(UserId, CountryId)` UNIQUE on active StateRepresentativeAssignment (filter `WHERE RevokedOn IS NULL`)
- **Read-pattern composite indexes** —
  - `Resource(CategoryId, PublishedOn DESC)` for category browse
  - `News(PublishedOn DESC)` for news listing
  - `Event(StartsOn)` for upcoming-events
  - `Post(TopicId, CreatedOn DESC)` for topic feed
  - `UserNotification(UserId, ReadOn, SentOn DESC)` for inbox
  - `AuditEvent(Actor, OccurredOn)` (existing from Phase 06)
- **Full-text indexes** (SQL Server FT catalog) —
  - `Resource(TitleAr, TitleEn, DescriptionAr, DescriptionEn)`
  - `News(TitleAr, TitleEn, ContentAr, ContentEn)`
  - `Page(TitleAr, TitleEn, ContentAr, ContentEn)`
  - `Country(NameAr, NameEn, RegionAr, RegionEn)`
  - `Topic(NameAr, NameEn, DescriptionAr, DescriptionEn)`

### 5.3 Cascade behavior

- **Hard cascades:** `User → ExpertProfile`, `Country → CountryProfile`, `Post → PostReply`, `Post → PostRating`, `KnowledgeMap → KnowledgeMapNode`, `KnowledgeMapNode → KnowledgeMapEdge` (as From or To), `CityScenario → CityScenarioResult`, `Topic → Post (set null)`, `NotificationTemplate → UserNotification (set null)`.
- **Restrict (no cascade):** anything else. Caller must clean up dependents explicitly.
- **Soft cascades:** when an aggregate root is soft-deleted, the domain method walks the aggregate and sets `IsDeleted=true` on children. Not via DB cascade — explicit in code.

### 5.4 SaveChangesInterceptor algorithm

```
SavingChangesAsync():
  1. snapshot CurrentUser claims (Sub, PreferredUsername, Upn) → resolved actor
  2. for each entry in ChangeTracker.Entries():
     if entry.Entity has [Audited] and state in {Added, Modified, Deleted}:
        compute diff:
          - Added: full property values JSON
          - Modified: only changed properties (Old → New)
          - Deleted: full pre-delete property values
        compute action: $"{EntityTypeName}.{state}"
        compute resource: $"{EntityTypeName}/{entry.Entity.Id}"
        new AuditEvent {
          Id = Guid.NewGuid(),
          OccurredOn = clock.UtcNow,
          Actor = actor,
          Action = action,
          Resource = resource,
          CorrelationId = currentCorrelationId,
          Diff = diffJson
        } → add to context's tracked entities (in same transaction)
  3. proceed with normal SaveChanges flow
```

`SavedChangesAsync()` (post-commit):
1. drain `DomainEvents` from each tracked aggregate root.
2. publish via `IPublisher.Publish(event)` per event.
3. clear domain events on each aggregate.

### 5.5 Soft-delete query-filter registration

```csharp
foreach (var entityType in modelBuilder.Model.GetEntityTypes())
{
    if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
    {
        var parameter = Expression.Parameter(entityType.ClrType, "e");
        var filter = Expression.Lambda(
            Expression.Not(Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted))),
            parameter);
        modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
    }
}
```

### 5.6 Permissions YAML format

```yaml
# CCE — Permission matrix
# Source of truth for both backend [Authorize(Policy=...)] and admin role-management UI.

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

**Source generator output:**
- `public const string Resource_Center_Upload = "Resource.Center.Upload"` etc.
- `Permissions.All` — `IReadOnlyList<string>` of all permission strings.
- `RolePermissionMap.SuperAdmin` — `IReadOnlyList<string>` of permissions assigned to SuperAdmin.
- One static collection per role (`SuperAdmin`, `ContentManager`, `StateRepresentative`, `CommunityExpert`, `RegisteredUser`, `Anonymous`).

**Role count:** 6 (5 named + 1 implicit `Anonymous`).

### 5.7 Seed data

Seeder runs idempotently in this order:

1. Roles from YAML (5 named roles).
2. `admin@cce.local` user with `SuperAdmin` role (matches Keycloak realm seed).
3. `Country` rows: ~25 countries with ISO codes + ar/en names from a YAML file at `backend/seed/countries.yaml`. No KAPSARC data (sub-project 8 populates).
4. `CityTechnology` rows: ~30 technologies from `backend/seed/city-technologies.yaml`.
5. `KnowledgeMap` + nodes/edges: 1 default "CCE Technology Map" with ~15 nodes from `backend/seed/knowledge-maps.yaml`.
6. `NotificationTemplate` rows: ~20 templates from BRD §7.1, content in `backend/seed/notification-templates.yaml`.
7. `Topic` rows: ~10 default community topics.
8. `ResourceCategory` rows: ~12 default categories.
9. `Page` rows: 3 placeholder pages (AboutPlatform, TermsOfService, PrivacyPolicy).

`--demo` flag adds: 3 News, 2 Events, 5 Resources (1 per category sample), 1 Post per topic. Demo data only for dev environment.

`--reset` flag drops + recreates the DB before seeding. Refuses to run if `ASPNETCORE_ENVIRONMENT=Production`.

CLI surface:
```
dotnet run --project backend/src/CCE.Infrastructure -- seed
dotnet run --project backend/src/CCE.Infrastructure -- seed --demo
dotnet run --project backend/src/CCE.Infrastructure -- seed --reset --demo
```

---

## 6. Data flow scenarios

### 6.1 Write flow with audit

`Application.CreatePostHandler` → `new Post(...)` → `_db.Posts.Add(post)` → `_db.SaveChangesAsync()` → AuditingInterceptor scans `[Audited]` → inserts AuditEvent in same TX → SavedChangesAsync drains domain events → publishes via MediatR → search-index updater + notification fan-out react.

### 6.2 Read with soft-delete

Default: query filter excludes `IsDeleted=true` rows. Admin recovery: `IgnoreQueryFilters()`.

### 6.3 KAPSARC snapshot consumption (sub-project 8 implementation)

Sub-project 2 ships the schema. Sub-project 8 ships the daily job that INSERTs `CountryKapsarcSnapshot` rows + UPDATEs `Country.LatestKapsarcSnapshotId`.

### 6.4 Bilingual content read

Domain entities expose both `*_ar` + `*_en` columns; API DTOs project the right one based on `Accept-Language` (Phase 08 LocalizationMiddleware sets `CultureInfo.CurrentCulture`).

### 6.5 Seeder

`dotnet run -- seed` → idempotent upserts → `--demo` adds dev-only data → `--reset` drops + recreates.

### 6.6 Permission check

`[Authorize(Policy = Permissions.Resource_Center_Upload)]` on a future endpoint (sub-projects 3+). Phase 08's PermissionPolicyRegistration registered one policy per `Permissions.All` entry. Source generator emits `RolePermissionMap.SuperAdmin` etc. The policy's authorization handler checks: user has any role X where `RolePermissionMap.<X>` contains the policy name.

---

## 7. Error handling

### 7.1 Domain invariants

Constructor + behavior-method guards throw `ArgumentException` / `InvalidOperationException`. Bubble to ExceptionHandlingMiddleware → 500 ProblemDetails. These are caller bugs, not user input.

### 7.2 Application validation

`AbstractValidator<T>` in MediatR pipeline → `FluentValidation.ValidationException` → middleware → 400 with field errors. Defense in depth alongside domain invariants.

### 7.3 EF constraint violations

| Violation | Mapped to | HTTP |
|---|---|---|
| Unique constraint | `DuplicateException` | 409 Conflict |
| FK violation | `InvalidOperationException` (caller bug) | 500 |
| Concurrency | `DbUpdateConcurrencyException` | 409 with etag-mismatch extension |
| Check constraint | `DomainException` | 400 |
| Append-only trigger (audit_events UPDATE/DELETE) | `InvalidOperationException` | 500 (should never happen) |

`DbExceptionMapper` is an MSSQL-aware mapper at `CCE.Infrastructure/Persistence/DbExceptionMapper.cs`.

### 7.4 Soft-delete edges

- Read of soft-deleted ID: query filter excludes → 404.
- FK to soft-deleted parent: child still exists; FK is non-null but parent is hidden — by design.
- Hard delete (GDPR): admin endpoint with `IgnoreQueryFilters()` + `ExecuteDelete()`. Audit interceptor captures via BeforeDelete hook.
- Restore: domain `Restore(byUserId, now)` flips IsDeleted; interceptor audits.

### 7.5 Audit interceptor failures

- Missing actor: falls back to `"system"` literal.
- Diff serialization throws: caught; row written with marker action `<diff-serialization-failed>`.
- Audit INSERT fails: whole TX rolls back. Audit and entity always agree.

### 7.6 Migration failures

- Per-migration TX: failed migration leaves DB at prior version.
- Schema parity test catches Azure SQL Edge vs SQL Server 2022 divergence in CI.

---

## 8. Testing strategy

Per ADR-0007 + Foundation gates: ≥90% Domain + Application, ≥70% Infrastructure + Api.

| Layer | Count target | Notes |
|---|---|---|
| Domain.Tests | ~120 | Per-aggregate invariants + state transitions + soft-delete + audit-attribute coverage test |
| Application.Tests | ~60 | Handler tests + ValidationBehavior + DbExceptionMapper |
| Infrastructure.Tests | ~40 | Migration round-trip, soft-delete filter, audit interceptor (success + failure modes), seeder idempotency, cascade soft-delete |
| Migration parity | 1 (heavy) | Testcontainers spin both engines; assert INFORMATION_SCHEMA equivalence |
| Architecture.Tests | ~15 | New project. Domain has no infra deps; Application has no DbContext deps; sealed aggregates; namespace placement |
| Source-generator tests | ~20 | New project. Nested YAML, role mappings, malformed YAML diagnostics |
| Api.IntegrationTests | +10 | Round-trip per bounded context |
| **Net new** | **~265** | |
| **Cumulative** | **~380** | |

### Migration parity test details

```csharp
[Trait("Category", "Slow")]
[Fact]
public async Task Migration_produces_identical_schema_on_AzureSqlEdge_and_SqlServer2022()
{
    await using var edge = new AzureSqlEdgeContainer(...);
    await using var sql = new MsSqlContainer(...);    // mssql/server:2022-latest
    await edge.StartAsync();
    await sql.StartAsync();

    await ApplyMigrations(edge.GetConnectionString());
    await ApplyMigrations(sql.GetConnectionString());

    var edgeSchema = await ReadSchemaAsync(edge);
    var sqlSchema = await ReadSchemaAsync(sql);

    edgeSchema.Tables.Should().BeEquivalentTo(sqlSchema.Tables, opt => opt.ExcludingEngineSpecificDifferences());
    edgeSchema.Indexes.Should().BeEquivalentTo(sqlSchema.Indexes, opt => opt.ExcludingEngineSpecificDifferences());
    edgeSchema.Constraints.Should().BeEquivalentTo(sqlSchema.Constraints, opt => opt.ExcludingEngineSpecificDifferences());
}
```

Skipped on local arm64 runs (`mcr.microsoft.com/mssql/server` is amd64-only). Runs on amd64 CI.

---

## 9. Definition of Done

Foundation's brief + brainstorm decisions translated to verifiable items.

### Schema + entities

1. All 36 entities exist under `CCE.Domain/<bounded-context>/`.
2. All 8 aggregate roots inherit `AggregateRoot<Guid>`.
3. All entities listed in §4.11 carry `[Audited]`.
4. All soft-deletable entities implement `ISoftDeletable`.
5. Domain has zero references to `EntityFrameworkCore`, `AspNetCore`, `MediatR`, `FluentValidation`, `Sentry`, or `Microsoft.Extensions.*` (verified by architecture test).
6. Every aggregate root is `sealed`.

### Persistence

7. `CceDbContext` extends `IdentityDbContext<User, Role, Guid>`.
8. Per-entity `IEntityTypeConfiguration<T>` for every entity, organized by bounded context folder.
9. `DataDomainInitial` migration creates all tables + indexes + FKs + full-text indexes + rowversion columns.
10. Soft-delete query filter registered for every `ISoftDeletable` entity.
11. `AuditingInterceptor` writes `AuditEvent` rows in same TX as the audited write.
12. `DomainEventDispatcher` post-commit hook publishes via MediatR.
13. `DbExceptionMapper` translates SQL errors to domain exceptions.

### Permissions

14. `permissions.yaml` covers all permissions in §5.6.
15. Source generator emits `Permissions.<Name>` constants + `RolePermissionMap.<Role>` collections.
16. `Permissions.All` enumerates all permissions.
17. Phase 08's `AddCcePermissionPolicies` (called from API Programs) registers one policy per `Permissions.All` entry.
18. Source generator handles malformed YAML with a diagnostic, not a crash.

### Seed data

19. `dotnet run -- seed` is idempotent (re-runnable safely).
20. `--reset` drops + recreates DB; refuses if env=Production.
21. `--demo` adds dev-only sample content.
22. Seeder loads roles from YAML, then `admin@cce.local`, then countries (~25), city techs (~30), knowledge map (~15 nodes), notification templates (~20), topics (~10), resource categories (~12), pages (3).

### Tests

23. `dotnet test backend/CCE.sln` reports ≥ ~380 tests passing.
24. Domain.Tests coverage ≥90%.
25. Application.Tests coverage ≥90% (for handlers shipped in this sub-project; sub-projects 3+ add more).
26. Infrastructure.Tests coverage ≥70%.
27. Architecture tests in `CCE.ArchitectureTests` enforce layering rules.
28. Migration parity test passes on amd64 (Azure SQL Edge ≡ SQL Server 2022).
29. Source generator tests cover nested YAML + role mappings.

### Documentation

30. ADR-0019 through ADR-0026 added to `docs/adr/` for the 8 brainstorm decisions.
31. `docs/subprojects/02-data-domain.md` updated with link to this spec + completion-report stub for sub-project 2's release.
32. `docs/requirements-trace.csv` updated: any BRD section now satisfied (in part) by sub-project 2 changes from `pending` to `in-progress` (entity exists) or `done`.

### Release

33. Tag `data-domain-v0.1.0` on `main` after all DoD items green.

---

## 10. Risks

| # | Risk | Mitigation |
|---|---|---|
| R1 | 36-entity schema is too large for one migration → reviewing it is hard | Migration is mechanical; design review focuses on entities + invariants in C#, not raw SQL. Migration parity test catches engine-specific bugs. |
| R2 | Demo data drift — if seed YAML evolves, dev DBs become inconsistent | Seeder is idempotent + `--reset` makes refresh trivial. Document `dev-reset.sh` workflow in README. |
| R3 | SaveChangesInterceptor performance impact on hot writes | Diff serialization is the cost; mitigation: only `[Audited]` entities pay it; high-volume non-audited entities (PostRating, Follow) skip. Budget: <5ms overhead per write on normal-sized payloads. |
| R4 | Soft-delete query filter forgotten on a query that needs all rows | EF makes IgnoreQueryFilters explicit; admin code paths are infrequent and reviewed. |
| R5 | EF Core 8 + Identity + many DbSets → SaveChanges slow on large transactions | Foundation is fine; if profile shows hotspots, partition large writes into smaller TXs. Sub-project 2's reads/writes are small. |
| R6 | Permission YAML role list drifts from Keycloak realm role list | Add a unit test: parse YAML role list, assert it matches the seeded Keycloak realm export role list (via fixture). Keeps the two in sync. |
| R7 | Full-text indexes not supported on Azure SQL Edge | Edge supports a subset; verify in migration parity test. If missing, mark FT indexes as `IF SUPPORTED` and add a runtime fallback (LIKE queries) — Smart Assistant is sub-project 7 anyway. |
| R8 | Sub-project 2 ships entities sub-projects 3-7 reshape | Coordinate via spec reviews per sub-project; the schema can absorb additive changes via migrations. Breaking changes get explicit deprecation. |

---

## 11. Open decisions (none)

All major decisions resolved through brainstorming. Implementation-level details (specific column types, exact index names) are settled during plan-writing.

---

## 12. Next step

After user approves this spec, transition to `superpowers:writing-plans` skill to produce the sub-project 2 implementation plan.

— End of spec —
