# Application Layer — Feature-Based Reorganization Plan

**Status:** Draft  
**Scope:** `src/CCE.Application/`  
**Goal:** Move from fragmented technical-type grouping (`Commands/`, `Queries/`, `Dtos/` at domain root) to **vertical feature slices** where each aggregate owns its commands, queries, DTOs, validators, and repository interfaces.

---

## 1. Current State

### 1.1 What's Working
- **Per-feature command folders** already exist: `Commands/CreateEvent/CreateEventCommand.cs` ✅  
- **Per-feature query folders** already exist: `Queries/GetEventById/GetEventByIdQuery.cs` ✅  
- Validators sit next to handlers: `CreateEventCommandValidator.cs` ✅  

### 1.2 What's Fragmented

```
Content/                              ← Domain root
├── Commands/CreateEvent/...           ← Good
├── Commands/UpdateEvent/...           ← Good
├── Queries/GetEventById/...           ← Good
├── Queries/ListEvents/...             ← Good
├── Dtos/EventDto.cs                   ← Far from commands/queries
├── Dtos/NewsDto.cs                    ← Same
├── Dtos/ResourceDto.cs                ← Same
├── IEventRepository.cs                ← At domain root
├── INewsRepository.cs                 ← At domain root
├── IFileStorage.cs                    ← Cross-cutting, also at root
└── Public/Dtos/PublicEventDto.cs      ← Parallel structure
```

**Problem:** DTOs and repository interfaces are grouped by *technical type* instead of by *business feature*. This causes:
- Cognitive overhead: to understand "Events", a developer jumps between `Commands/`, `Queries/`, `Dtos/`, and root-level interfaces.
- Namespace sprawl: `using CCE.Application.Content.Dtos;` imports every DTO in the domain.
- Merge conflicts: `Dtos/` and `Queries/` folders are hotspots because every feature touches them.

---

## 2. Target Structure (Vertical Slices)

### 2.1 Guiding Principle
**Each aggregate is a self-contained folder containing everything it needs.**

- Commands the aggregate accepts  
- Queries the aggregate supports  
- DTOs it exposes  
- Repository interface it declares  
- Public-facing variants (if any)

Cross-cutting interfaces (used by *multiple* aggregates) stay at domain root or in `Shared/`.

### 2.2 Example: Content Domain

```
Content/
│
├── Events/                          ← Aggregate / Feature
│   ├── Commands/
│   │   ├── CreateEvent/
│   │   │   ├── CreateEventCommand.cs
│   │   │   ├── CreateEventCommandHandler.cs
│   │   │   └── CreateEventCommandValidator.cs
│   │   ├── UpdateEvent/
│   │   ├── DeleteEvent/
│   │   ├── RescheduleEvent/
│   │   └── PublishEvent/
│   ├── Queries/
│   │   ├── GetEventById/
│   │   │   ├── GetEventByIdQuery.cs
│   │   │   └── GetEventByIdQueryHandler.cs
│   │   └── ListEvents/
│   ├── Dtos/
│   │   └── EventDto.cs
│   └── IEventRepository.cs
│
├── News/
│   ├── Commands/
│   │   ├── CreateNews/
│   │   ├── UpdateNews/
│   │   ├── DeleteNews/
│   │   └── PublishNews/
│   ├── Queries/
│   │   ├── GetNewsById/
│   │   └── ListNews/
│   ├── Dtos/
│   │   └── NewsDto.cs
│   └── INewsRepository.cs
│
├── Resources/
│   ├── Commands/
│   │   ├── CreateResource/
│   │   ├── UpdateResource/
│   │   └── PublishResource/
│   ├── Queries/
│   │   ├── GetResourceById/
│   │   └── ListResources/
│   ├── Dtos/
│   │   └── ResourceDto.cs
│   └── IResourceRepository.cs
│
├── Pages/
│   ├── Commands/
│   ├── Queries/
│   ├── Dtos/
│   └── IPageRepository.cs
│
├── ResourceCategories/
│   ├── Commands/
│   ├── Queries/
│   ├── Dtos/
│   └── IResourceCategoryRepository.cs
│
├── HomepageSections/
│   ├── Commands/
│   ├── Queries/
│   ├── Dtos/
│   └── IHomepageSectionRepository.cs
│
├── Assets/
│   ├── Commands/
│   │   └── UploadAsset/
│   ├── Queries/
│   │   └── GetAssetById/
│   ├── Dtos/
│   │   └── AssetFileDto.cs
│   └── IAssetRepository.cs
│
├── CountryResourceRequests/
│   ├── Commands/
│   │   ├── ApproveCountryResourceRequest/
│   │   └── RejectCountryResourceRequest/
│   ├── Dtos/
│   │   └── CountryResourceRequestDto.cs
│   └── ICountryResourceRequestRepository.cs
│
├── Public/                           ← External-facing APIs
│   ├── Dtos/
│   │   ├── PublicEventDto.cs
│   │   ├── PublicNewsDto.cs
│   │   ├── PublicPageDto.cs
│   │   ├── PublicResourceDto.cs
│   │   ├── PublicResourceCategoryDto.cs
│   │   ├── PublicHomepageSectionDto.cs
│   │   └── IcsBuilder.cs
│   └── Queries/
│       ├── GetPublicEventById/
│       ├── ListPublicEvents/
│       ├── GetPublicNewsBySlug/
│       ├── ListPublicNews/
│       ├── GetPublicPageBySlug/
│       ├── GetPublicResourceById/
│       ├── ListPublicResources/
│       ├── ListPublicResourceCategories/
│       └── ListPublicHomepageSections/
│
└── Shared/                           ← Cross-cutting within Content
    ├── IFileStorage.cs
    └── IClamAvScanner.cs
```

### 2.3 Example: Identity Domain

```
Identity/
│
├── Auth/                           ← Already reorganized ✅
│   ├── Common/
│   ├── Register/
│   ├── Login/
│   ├── RefreshToken/
│   ├── ForgotPassword/
│   ├── ResetPassword/
│   └── Logout/
│
├── Users/
│   ├── Queries/
│   │   ├── GetUserById/
│   │   └── ListUsers/
│   └── Dtos/
│       ├── UserDetailDto.cs
│       └── UserListItemDto.cs
│
├── ExpertWorkflow/
│   ├── Commands/
│   │   ├── ApproveExpertRequest/
│   │   └── RejectExpertRequest/
│   ├── Queries/
│   │   ├── ListExpertRequests/
│   │   └── ListExpertProfiles/
│   ├── Dtos/
│   │   ├── ExpertRequestDto.cs
│   │   └── ExpertProfileDto.cs
│   └── IExpertWorkflowRepository.cs
│
├── StateRepAssignments/
│   ├── Commands/
│   │   ├── CreateStateRepAssignment/
│   │   └── RevokeStateRepAssignment/
│   ├── Queries/
│   │   └── ListStateRepAssignments/
│   ├── Dtos/
│   │   └── StateRepAssignmentDto.cs
│   └── IStateRepAssignmentRepository.cs
│
├── Roles/
│   └── Commands/
│       └── AssignUserRoles/
│           ├── AssignUserRolesCommand.cs
│           ├── AssignUserRolesCommandHandler.cs
│           ├── AssignUserRolesCommandValidator.cs
│           └── AssignUserRolesRequest.cs
│
├── Public/
│   ├── Commands/
│   │   ├── SubmitExpertRequest/
│   │   └── UpdateMyProfile/
│   ├── Queries/
│   │   ├── GetMyProfile/
│   │   └── GetMyExpertStatus/
│   └── Dtos/
│       ├── UserProfileDto.cs
│       └── ExpertRequestStatusDto.cs
│
├── IUserSyncRepository.cs          ← Cross-user concerns
├── IUserRoleAssignmentRepository.cs
└── ICountryProfileService.cs         ← Move to Country?
```

### 2.4 Example: Community Domain

```
Community/
│
├── Posts/
│   ├── Commands/
│   │   ├── CreatePost/
│   │   ├── SoftDeletePost/
│   │   ├── MarkPostAnswered/
│   │   ├── RatePost/
│   │   ├── FollowPost/
│   │   └── UnfollowPost/
│   ├── Queries/
│   │   ├── ListAdminPosts/
│   │   └── AdminPostRow.cs
│   └── Dtos/
│       └── PostDto.cs          ← (to be created if needed)
│
├── Topics/
│   ├── Commands/
│   │   ├── CreateTopic/
│   │   ├── UpdateTopic/
│   │   ├── DeleteTopic/
│   │   ├── FollowTopic/
│   │   └── UnfollowTopic/
│   ├── Queries/
│   │   ├── GetTopicById/
│   │   └── ListTopics/
│   └── Dtos/
│       └── TopicDto.cs         ← Move from Community/Dtos/
│
├── Replies/
│   ├── Commands/
│   │   ├── CreateReply/
│   │   ├── EditReply/
│   │   └── SoftDeleteReply/
│   └── Dtos/
│       └── ReplyDto.cs         ← (to be created if needed)
│
├── Follows/
│   ├── Commands/
│   │   ├── FollowUser/
│   │   └── UnfollowUser/
│   └── Queries/
│       └── GetMyFollows/
│
├── Public/
│   ├── Queries/
│   │   ├── GetPublicPostById/
│   │   ├── ListPublicPostsInTopic/
│   │   ├── ListPublicPostReplies/
│   │   ├── GetPublicTopicBySlug/
│   │   └── ListPublicTopics/
│   └── Dtos/
│       ├── PublicPostDto.cs
│       ├── PublicPostReplyDto.cs
│       ├── PublicTopicDto.cs
│       └── MyFollowsDto.cs
│
└── Services/
    ├── ICommunityModerationService.cs
    ├── ICommunityWriteService.cs
    └── ITopicService.cs
```

### 2.5 Example: Country Domain

Merge `Country/` and `CountryPublic/` into a single coherent domain:

```
Country/
│
├── Countries/
│   ├── Commands/
│   │   └── UpdateCountry/
│   ├── Queries/
│   │   ├── GetCountryById/
│   │   └── ListCountries/
│   └── Dtos/
│       └── CountryDto.cs
│
├── CountryProfiles/
│   ├── Commands/
│   │   └── UpsertCountryProfile/
│   ├── Queries/
│   │   └── GetCountryProfile/
│   └── Dtos/
│       └── CountryProfileDto.cs
│
├── Public/
│   ├── Queries/
│   │   ├── GetPublicCountryProfile/
│   │   └── ListPublicCountries/
│   └── Dtos/
│       ├── PublicCountryDto.cs
│       └── PublicCountryProfileDto.cs
│
└── Services/
    ├── ICountryAdminService.cs
    └── ICountryProfileService.cs
```

### 2.6 Example: Notifications Domain

```
Notifications/
│
├── Templates/
│   ├── Commands/
│   │   ├── CreateNotificationTemplate/
│   │   └── UpdateNotificationTemplate/
│   ├── Queries/
│   │   ├── GetNotificationTemplateById/
│   │   └── ListNotificationTemplates/
│   ├── Dtos/
│   │   └── NotificationTemplateDto.cs
│   └── INotificationTemplateService.cs
│
├── UserNotifications/
│   ├── Queries/
│   │   ├── GetMyUnreadCount/
│   │   └── ListMyNotifications/
│   └── Dtos/
│       └── UserNotificationDto.cs
│
└── Public/
    ├── Commands/
    │   ├── MarkNotificationRead/
    │   └── MarkAllNotificationsRead/
    └── IUserNotificationService.cs
```

---

## 3. Cross-Cutting Domains (Stay Mostly As-Is)

These domains are small enough or already well-organized:

| Domain | Current State | Action |
|--------|---------------|--------|
| `Assistant/` | 1 command + interfaces | Keep; small |
| `Audit/` | 1 query + 1 DTO | Keep; small |
| `Health/` | 2 queries + 2 DTOs | Keep; small |
| `Kapsarc/` | 1 query + 1 DTO | Keep; small |
| `KnowledgeMaps/` | Public queries only | Keep; small |
| `Localization/` | 2 interfaces | Keep; small |
| `Reports/` | Service interfaces + row DTOs | Keep `Rows/` subfolder; organize services into `Services/` if more than 3 |
| `Search/` | 1 query + interfaces + DTOs | Keep; small |
| `Surveys/` | 1 command + 1 service | Keep; small |
| `InteractiveCity/` | Already per-feature ✅ | Keep as-is |

---

## 4. Namespace Strategy

| File Location | Namespace |
|---------------|-----------|
| `Content/Events/Commands/CreateEvent/CreateEventCommand.cs` | `CCE.Application.Content.Events.Commands.CreateEvent` |
| `Content/Events/Dtos/EventDto.cs` | `CCE.Application.Content.Events.Dtos` |
| `Content/Events/IEventRepository.cs` | `CCE.Application.Content.Events` |
| `Content/Public/Dtos/PublicEventDto.cs` | `CCE.Application.Content.Public.Dtos` |
| `Content/Shared/IFileStorage.cs` | `CCE.Application.Content.Shared` |
| `Common/Behaviors/ValidationBehavior.cs` | `CCE.Application.Common.Behaviors` |

**Rule:** The namespace mirrors the folder path under `CCE.Application`.

---

## 5. Command vs Request DTOs

### 5.1 Current Pattern
Some features have both a `Command` (for MediatR) and a `Request` (for endpoint binding):

```
CreateEventCommand.cs      → internal fields
CreateEventRequest.cs      → HTTP body shape (often identical)
```

### 5.2 Consolidation Rule
- **If identical**: Delete the `Request` type; bind endpoints directly to `Command`.
- **If endpoint injects extra fields** (`IpAddress`, `UserAgent`, `CurrentUserId`, etc.): Keep both. Endpoint creates `Command` from `Request + injected fields`.
- **If using `[FromRoute]` / `[FromQuery]`**: Keep `Request` for explicit binding.

---

## 6. Interface Organization

### 6.1 Repository Interfaces
**1-to-1 with an aggregate** → live inside the aggregate folder:

- `Content/Events/IEventRepository.cs`  
- `Content/News/INewsRepository.cs`  
- `Identity/ExpertWorkflow/IExpertWorkflowRepository.cs`

### 6.2 Service Interfaces (Orchestration)
**Coordinate multiple aggregates** → live in `Domain/Services/` or domain root:

- `Community/Services/ICommunityModerationService.cs`
- `Reports/Services/IUserRegistrationsReportService.cs`

### 6.3 Cross-Domain Interfaces
**Used by multiple domains** → stay in `Common/`:

- `Common/Interfaces/ICceDbContext.cs`
- `Common/Interfaces/ICurrentUserAccessor.cs`
- `Common/Interfaces/IEmailSender.cs`

---

## 7. Phased Rollout

Because this touches 250+ files, we roll out in phases. Each phase is a single PR.

### Phase 1: Content Domain (Pilot)
**Features:** Events, News, Resources, Pages, ResourceCategories, HomepageSections, Assets, CountryResourceRequests  
**Risk:** Medium — touches many endpoints and DTOs  
**Deliverable:** Working build + passing unit tests  
**Steps:**
1. Create new feature folders.
2. Move DTOs from `Content/Dtos/` into `Content/{Feature}/Dtos/`.
3. Move repository interfaces from `Content/` root into `Content/{Feature}/`.
4. Move commands/queries (already per-feature, just nest under `{Feature}/`).
5. Move `Public/` queries/DTOs into `Content/Public/` (already there, just verify).
6. Move cross-cutting interfaces (`IFileStorage`, `IClamAvScanner`) into `Content/Shared/`.
7. Update `using` statements in:
   - `CCE.Api.Internal/Endpoints/ContentEndpoints.cs`
   - `CCE.Api.External/Endpoints/PagesPublicEndpoints.cs` etc.
   - `CCE.Infrastructure/` repository implementations
   - `tests/CCE.Application.Tests/`
8. Delete empty `Content/Commands/`, `Content/Queries/`, `Content/Dtos/` folders.
9. Build & test.

### Phase 2: Identity Domain
**Features:** Auth (done ✅), Users, ExpertWorkflow, StateRepAssignments, Roles, Public  
**Risk:** Low-Medium — Auth already sliced  
**Steps:**
1. Merge `Identity/Dtos/` into `Identity/{Feature}/Dtos/`.
2. Move `IExpertWorkflowRepository.cs`, `IStateRepAssignmentRepository.cs`, `IUserSyncRepository.cs`, etc. into respective feature folders.
3. Move `Identity/Commands/` into `Identity/{Feature}/Commands/`.
4. Move `Identity/Queries/` into `Identity/{Feature}/Queries/`.
5. Move `Identity/Public/` into `Identity/Public/` (already there, verify structure).
6. Update `using` statements in API endpoints and Infrastructure.
7. Delete empty `Identity/Commands/`, `Identity/Queries/`, `Identity/Dtos/` folders.
8. Build & test.

### Phase 3: Community Domain
**Features:** Posts, Topics, Replies, Follows  
**Risk:** Medium — many commands, shared DTOs  
**Steps:** Same pattern as Phase 1.

### Phase 4: Country + Notifications + Remaining
**Features:** Country (merge `CountryPublic`), Notifications, InteractiveCity, KnowledgeMaps  
**Risk:** Low — smaller domains  
**Steps:**
1. Merge `CountryPublic/` into `Country/Public/`.
2. Slice Notifications into `Templates/` + `UserNotifications/`.
3. Verify InteractiveCity and KnowledgeMaps already follow the pattern.
4. Build & test.

---

## 8. File-Level Migration (Phase 1 — Content)

### 8.1 Source → Destination Map

| Current | New Home |
|---------|----------|
| `Content/Dtos/EventDto.cs` | `Content/Events/Dtos/EventDto.cs` |
| `Content/Dtos/NewsDto.cs` | `Content/News/Dtos/NewsDto.cs` |
| `Content/Dtos/ResourceDto.cs` | `Content/Resources/Dtos/ResourceDto.cs` |
| `Content/Dtos/PageDto.cs` | `Content/Pages/Dtos/PageDto.cs` |
| `Content/Dtos/ResourceCategoryDto.cs` | `Content/ResourceCategories/Dtos/ResourceCategoryDto.cs` |
| `Content/Dtos/HomepageSectionDto.cs` | `Content/HomepageSections/Dtos/HomepageSectionDto.cs` |
| `Content/Dtos/AssetFileDto.cs` | `Content/Assets/Dtos/AssetFileDto.cs` |
| `Content/Dtos/CountryResourceRequestDto.cs` | `Content/CountryResourceRequests/Dtos/CountryResourceRequestDto.cs` |
| `Content/IEventRepository.cs` | `Content/Events/IEventRepository.cs` |
| `Content/INewsRepository.cs` | `Content/News/INewsRepository.cs` |
| `Content/IResourceRepository.cs` | `Content/Resources/IResourceRepository.cs` |
| `Content/IPageRepository.cs` | `Content/Pages/IPageRepository.cs` |
| `Content/IResourceCategoryRepository.cs` | `Content/ResourceCategories/IResourceCategoryRepository.cs` |
| `Content/IHomepageSectionRepository.cs` | `Content/HomepageSections/IHomepageSectionRepository.cs` |
| `Content/IAssetRepository.cs` | `Content/Assets/IAssetRepository.cs` |
| `Content/ICountryResourceRequestRepository.cs` | `Content/CountryResourceRequests/ICountryResourceRequestRepository.cs` |
| `Content/IFileStorage.cs` | `Content/Shared/IFileStorage.cs` |
| `Content/IClamAvScanner.cs` | `Content/Shared/IClamAvScanner.cs` |
| `Content/Commands/CreateEvent/*` | `Content/Events/Commands/CreateEvent/*` |
| `Content/Commands/UpdateEvent/*` | `Content/Events/Commands/UpdateEvent/*` |
| `Content/Commands/DeleteEvent/*` | `Content/Events/Commands/DeleteEvent/*` |
| `Content/Commands/RescheduleEvent/*` | `Content/Events/Commands/RescheduleEvent/*` |
| `Content/Commands/PublishNews/*` | `Content/News/Commands/PublishNews/*` |
| `Content/Queries/GetEventById/*` | `Content/Events/Queries/GetEventById/*` |
| `Content/Queries/ListEvents/*` | `Content/Events/Queries/ListEvents/*` |
| `Content/Public/Dtos/*` | `Content/Public/Dtos/*` (no change needed) |
| `Content/Public/Queries/*` | `Content/Public/Queries/*` (no change needed) |
| `Content/Public/IcsBuilder.cs` | `Content/Public/IcsBuilder.cs` |
| `Content/Public/IResourceViewCountRepository.cs` | `Content/Shared/IResourceViewCountRepository.cs` |

### 8.2 Consumers to Update

| Consumer File | What to Update |
|---------------|----------------|
| `src/CCE.Api.Internal/Endpoints/ContentEndpoints.cs` | `using CCE.Application.Content.Dtos;` → feature namespaces |
| `src/CCE.Api.External/Endpoints/EventsPublicEndpoints.cs` | `using CCE.Application.Content.Dtos;` → feature namespaces |
| `src/CCE.Api.External/Endpoints/PagesPublicEndpoints.cs` | Same |
| `src/CCE.Api.External/Endpoints/ResourcesPublicEndpoints.cs` | Same |
| `src/CCE.Infrastructure/Content/*Repository.cs` | `using CCE.Application.Content;` → `CCE.Application.Content.Events`, etc. |
| `tests/CCE.Application.Tests/Content/*` | Update test namespaces and usings |

---

## 9. Validation Criteria

After each phase:

1. **Build:** `dotnet build CCE.sln` — must pass with 0 warnings (TreatWarningsAsErrors=true).
2. **Unit tests:** `dotnet test tests/CCE.Application.Tests` — must pass.
3. **No orphaned files:** Delete empty `Commands/`, `Queries/`, `Dtos/` folders after migration.
4. **No duplicate DTOs:** If a DTO is used by two features (rare), it lives in the feature that owns the aggregate and is `internal` or stays in `Shared/`.
5. **Namespace check:** Every new file's namespace matches its folder path.

---

## 10. Open Decisions

1. **Should `Public/` DTOs be nested inside each feature?**  
   - Option A: `Content/Events/Public/PublicEventDto.cs` (fully nested)  
   - Option B: `Content/Public/Dtos/PublicEventDto.cs` (centralized, current)  
   - **Recommendation:** Keep Option B. Public APIs are a separate bounded context and having them in one place makes it easy to see the external contract.

2. **Should `Request` types be eliminated where they mirror `Command` exactly?**  
   - **Recommendation:** Yes. Remove `CreateEventRequest`, `UpdateEventRequest`, etc. where identical. The endpoint can bind directly to the Command. This reduces file count and eliminates a class of drift bugs.

3. **Should `Rows/` in Reports move to `Reports/Services/Rows/` or stay?**  
   - **Recommendation:** Keep `Reports/Rows/` as-is or rename to `Reports/Dtos/` for consistency. If report services grow, create `Reports/Services/`.

---

## 11. Summary

| Metric | Before | After |
|--------|--------|-------|
| DTO location | `Domain/Dtos/` (fragmented) | `Domain/Feature/Dtos/` (co-located) |
| Repository interfaces | Domain root | Inside owning aggregate |
| Cognitive load to find "Events" | 4+ folders | 1 folder |
| Merge-conflict hotspots | `Dtos/`, `Queries/` | Distributed across features |
| Namespace granularity | Broad | Precise |

This plan turns the Application layer into a **screaming architecture**: open any folder and immediately understand what the system does.
