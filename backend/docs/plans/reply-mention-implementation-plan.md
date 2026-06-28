# Reply Mention — Implementation Plan

## Status: SHIPPED ✅

All phases implemented and migration applied (`20260625112202_AddMentionDenormalizedFields`).

---

## Architecture decisions (applied)

| # | Decision | Rationale |
|---|----------|-----------|
| 1 | **`MentionService`** extracted into `CCE.Application/Community/Services/` | Prevents duplication between `CreateReplyCommandHandler` and `PublishPostCommandHandler` |
| 2 | **Tier 3 global search cut** — autocomplete is followers + community members only | Privacy; global user enumeration is a security/privacy risk |
| 3 | **Server-side mention parsing** — `@[userId:name]` regex in `MentionService`, no client-provided `MentionedUserIds` | Prevents spam via arbitrary client IDs |
| 4 | **`CommunityId` added to `Mention` entity** | Avoids joins through Post on every mention query |
| 5 | **`Snippet` stored at write time on `Mention` row** (120 chars) | Avoids runtime joins to Post in `ListMyMentions` |

---

## Domain entity — `Mention`

Added three properties:

```csharp
public Guid PostId { get; private set; }       // always root post
public Guid CommunityId { get; private set; }  // denormalized
public string Snippet { get; private set; }    // first 120 chars of source content
```

Factory signature:

```csharp
Mention.Create(sourceType, sourceId, postId, communityId, snippet, mentionedUserId, mentionedByUserId, clock)
```

---

## Mention tag syntax

Content must embed mentions as: `@[userId:displayName]`  
Example: `Hello @[3fa85f64-5717-4562-b3fc-2c963f66afa6:Alice]`

The regex in `MentionService` extracts the UUID from group 1. Tags with invalid UUIDs or the author's own ID are silently dropped. Cap: 10 per source.

---

## Files shipped

### New
| File | Purpose |
|------|---------|
| `Application/Community/Services/IMentionService.cs` | Interface |
| `Application/Community/Services/MentionService.cs` | Parse, validate, cap, persist |
| `Application/Community/Public/Dtos/MentionableUserDto.cs` | Autocomplete DTO |
| `Application/Community/Public/Queries/GetMentionableUsers/GetMentionableUsersQuery.cs` | Query |
| `Application/Community/Public/Queries/GetMentionableUsers/GetMentionableUsersQueryHandler.cs` | Handler |
| `Infrastructure/Persistence/Migrations/20260625112202_AddMentionDenormalizedFields.cs` | DB migration |

### Modified
| File | Change |
|------|--------|
| `Domain/Community/Mention.cs` | Added `PostId`, `CommunityId`, `Snippet` |
| `Infrastructure/Persistence/Configurations/Community/MentionConfiguration.cs` | Column + index config for new fields |
| `Application/Community/Commands/CreateReply/CreateReplyCommand.cs` | Removed `MentionedUserIds` |
| `Application/Community/Commands/CreateReply/CreateReplyRequest.cs` | Removed `MentionedUserIds` |
| `Application/Community/Commands/CreateReply/CreateReplyCommandHandler.cs` | Uses `IMentionService`, adds Push channel |
| `Application/Community/Commands/PublishPost/PublishPostCommand.cs` | Added `Locale` parameter |
| `Application/Community/Commands/PublishPost/PublishPostCommandHandler.cs` | Full mention support added |
| `Application/Community/IReplyRepository.cs` | Added `SearchMentionableAsync` |
| `Application/Community/Public/Dtos/MyMentionDto.cs` | Enriched with names + snippet |
| `Application/Community/Public/Queries/ListMyMentions/ListMyMentionsQueryHandler.cs` | Join users for names |
| `Application/DependencyInjection.cs` | Registered `MentionService` |
| `Api.External/Endpoints/CommunityWriteEndpoints.cs` | Removed `MentionedUserIds` from CreateReply call |
| `Api.External/Endpoints/CommunityPublicEndpoints.cs` | Added `GET /api/community/communities/{id}/mentionable-users` |
| `Infrastructure/Community/ReplyRepository.cs` | Implemented 2-tier `SearchMentionableAsync` |

---

## Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `POST` | `/api/community/posts/{id}/replies` | `Community_Post_Reply` | Create reply; mentions parsed server-side from content |
| `POST` | `/api/community/posts/{id}/publish` | `Community_Post_Create` | Publish draft; mentions parsed from post body |
| `GET` | `/api/community/communities/{id}/mentionable-users?q=rash&limit=10` | `Community_Post_Reply` | @-mention autocomplete (2 tiers) |
| `GET` | `/api/me/mentions` | authenticated | List my mentions (enriched with names + snippet) |

---

## Notification template needed

The `COMMUNITY_MENTION` template must be seeded for both `InApp` and `Push` channels:

```csharp
new NotificationTemplate
{
    TemplateCode = "COMMUNITY_MENTION",
    EventType    = NotificationEventType.CommunityUserMentioned,
    Channel      = NotificationChannel.InApp,   // duplicate for Push
    TitleAr      = "تم ذكرك",
    TitleEn      = "You were mentioned",
    BodyAr       = "ذكرك {{MentionedByName}} في تعليق",
    BodyEn       = "{{MentionedByName}} mentioned you in a comment",
    IsActive     = true,
}
```

Seed via Internal API: `POST /api/notification-templates`.

---

## Rule going forward

- `MentionService` is the **only** place mention tags are parsed, validated, and persisted.
- Clients embed mentions as `@[uuid:name]` in rich-text content. No separate `MentionedUserIds` list.
- Cap is 10 mentions per source (enforced in `MentionService.ExtractAndPersistAsync`).
