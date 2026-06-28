# SignalR Improvement Plan — Community Social
**Target consumers:** Angular web, Flutter mobile  
**Branch:** `feat/signalr-hardening`  

> **Revision:** Validated against source on 2026-06-23. Fixes applied: §1.2 envelope now covers `ReceiveNotification`/`PresenceChanged`/`TypingChanged` (were un-wrapped); §1.2 `Title` is an explicit 3-file change, not a one-liner (no `Title` on `PostCreatedIntegrationEvent`); §1.3 reply `VoteChanged` also gets `downvoteCount` (was wrongly marked "replies only track upvotes"); §2.1 `reply.Body` → `reply.Content` (compile fix); §4.1 multi-instance MemoryCache caveat; "What does NOT change" updated for the shared hub on both APIs (Option 2).

---

## Guiding principle

Mobile pays for every HTTP round-trip in latency, battery, and connection teardown overhead.  
The fix is to make every SignalR push carry enough to **render without a follow-up GET**.  
Where the payload is inherently too large (full feed card), we push a toast trigger and let the user decide to load.

---

## Refetch vs Map — final decision table

| Event | Group | Decision | Reason |
|---|---|---|---|
| `VoteChanged` (post) | `post:{id}` | **Map** | Sends counts; add `downvoteCount` (Phase 1) |
| `VoteChanged` (reply) | `post:{id}` | **Map** | Same shape as post variant, add `downvoteCount` (Phase 1) |
| `PresenceChanged` | `post:{id}` | **Map** | Complete — viewer count only |
| `TypingChanged` | `post:{id}` | **Map** | Complete — user + bool |
| `PostModerated` | `post:{id}` + `community:{id}` | **Map** | Tombstone by `action` field |
| `ContentModerated` | `moderation` | **Map** | Complete for moderation queue |
| `PollResultsChanged` | `post:{id}` | **Map** (after Phase 1) | Options are in memory at save time — free to include |
| `NewReply` | `post:{id}` | **Map** (after Phase 2) | Fatten with body + author via one PK lookup |
| `ReceiveNotification` | `user:{id}` | **Map** (after Phase 2) | Add `actorId` + `metaData` to domain entity |
| `NewPost` | `community:{id}` + `topic:{id}` | **Toast + lazy refetch** | Full feed card needs tags, attachments, expert status — too large to push |

---

## Phase 1 — Wire contract (do before any frontend integration)

These are breaking changes to the wire format. Fix them before the frontend writes any `connection.on(...)` handlers.

### 1.1 Enforce camelCase

**File:** `src/CCE.Api.Common/SignalR/SignalRRegistration.cs`

```csharp
// Before
var builder = services.AddSignalR().AddJsonProtocol();

// After
var builder = services.AddSignalR()
    .AddJsonProtocol(o =>
        o.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);
```

No other files need changing. Anonymous object property names that were already explicitly lowercased (`postId = ...`, `replyId = ...`) remain correct. Record property names (PascalCase) will be lowercased by the policy automatically.

**Result:** Every event on the wire becomes consistently camelCase.

---

### 1.2 Add event envelope

Every push gets wrapped in a common envelope. `eventId` is a **dedup key only** (random GUID, not monotonic); clients must order events by `occurredOn`. `occurredOn` doubles as the `since` cursor for catch-up (Phase 3).

**File:** `src/CCE.Application/Common/Realtime/RealtimePayloads.cs` — add at top:

```csharp
/// <summary>
/// Outer wrapper for every server→client push. Gives the client an eventId for dedup,
/// a timestamp for ordering, and a stable nesting shape so payload schemas can evolve
/// independently of the envelope.
/// </summary>
public sealed record RealtimeEnvelope(
    System.Guid EventId,
    System.DateTimeOffset OccurredOn,
    object Payload)
{
    public static RealtimeEnvelope Wrap(object payload) =>
        new(System.Guid.NewGuid(), System.DateTimeOffset.UtcNow, payload);
}
```

`Wrap(...)` lives on the envelope itself (static method), so every publisher and the hub share one factory — no per-class private copy.

**File:** `src/CCE.Infrastructure/Notifications/CommunityRealtimePublisher.cs` — apply `RealtimeEnvelope.Wrap` in all four publish methods:

```csharp
// Apply in every SendAsync call — example for PublishToPostAsync:
await _hub.Clients.Group(RealtimeGroups.Post(postId))
    .SendAsync(eventName, RealtimeEnvelope.Wrap(payload), ct).ConfigureAwait(false);
```

Apply the same `RealtimeEnvelope.Wrap(payload)` change to `PublishToCommunityAsync`, `PublishToTopicAsync`, and `PublishToModeratorsAsync`.

**File:** `src/CCE.Infrastructure/Notifications/SignalRNotificationPublisher.cs` — `ReceiveNotification` is published directly (not via `CommunityRealtimePublisher`), so wrap here too:

```csharp
await _hubContext.Clients.User(notification.UserId.ToString())
    .SendAsync(RealtimeEvents.ReceiveNotification,
        RealtimeEnvelope.Wrap(new { /* ...existing notification fields + Phase-2 actor/metaData... */ }),
        cancellationToken)
    .ConfigureAwait(false);
```

**File:** `src/CCE.Infrastructure/Notifications/NotificationsHub.cs` — `PresenceChanged` and `TypingChanged` are broadcast directly from the hub, not via the publisher. Wrap them so the envelope contract is uniform across every event:

```csharp
// BroadcastPresenceAsync
return Clients.Group(RealtimeGroups.Post(postId))
    .SendAsync(RealtimeEvents.PresenceChanged,
        RealtimeEnvelope.Wrap(new PresenceChangedRealtime(postId, viewers)));

// BroadcastTypingAsync
return Clients.OthersInGroup(RealtimeGroups.Post(postId))
    .SendAsync(RealtimeEvents.TypingChanged,
        RealtimeEnvelope.Wrap(new TypingChangedRealtime(postId, userId, isTyping)));
```

**After this section, every server→client push is enveloped:** `ReceiveNotification`, `NewReply`, `VoteChanged`, `PollResultsChanged`, `NewPost`, `PostModerated`, `ContentModerated`, `PresenceChanged`, `TypingChanged`. Clients parse one shape.

> **Note on `eventId` semantics:** `Guid.NewGuid()` is random, **not** monotonic — do NOT use it for ordering. Clients must order events by `occurredOn`; `eventId` is solely a dedup key (store the last N seen, drop duplicates on reconnect).

**File:** `src/CCE.Infrastructure/Notifications/Messaging/Consumers/SignalRConsumer.cs` — wrap the `NewPost` push.

**Prerequisite:** `evt.Title` does **not** exist on `PostCreatedIntegrationEvent` today (fields: `PostId, CommunityId, TopicId, AuthorId, PublishedOn, Locale`). Adding it requires touching three files — schedule this as its own task:

1. **`src/CCE.Application/Common/Messaging/IntegrationEvents/PostCreatedIntegrationEvent.cs`** — add `string Title` to the record.
2. **`src/CCE.Application/Community/EventHandlers/PostCreatedBusPublisher.cs`** — pass `post.Title` when constructing the event.
3. **`src/CCE.Infrastructure/Notifications/Messaging/Consumers/SignalRConsumer.cs`** — include `Title` in the wrapped payload:

```csharp
var envelope = RealtimeEnvelope.Wrap(new
{
    evt.PostId,
    evt.CommunityId,
    evt.TopicId,
    evt.AuthorId,
    evt.PublishedOn,
    evt.Title,          // ← now available after step 1
});

await _hub.Clients.Group(RealtimeGroups.Community(evt.CommunityId))
    .SendAsync(RealtimeEvents.NewPost, envelope, ct).ConfigureAwait(false);

await _hub.Clients.Group(RealtimeGroups.Topic(evt.TopicId))
    .SendAsync(RealtimeEvents.NewPost, envelope, ct).ConfigureAwait(false);
```

**Wire shape after Phase 1:**

```json
{
  "eventId": "3fa85f64-...",
  "occurredOn": "2026-06-23T09:14:22.123Z",
  "payload": {
    "postId": "...",
    "upvoteCount": 12,
    "downvoteCount": 3,
    "score": 9
  }
}
```

Client reads: `connection.on("VoteChanged", (envelope) => { const p = envelope.payload; ... })`

---

### 1.3 Fix VoteChanged — add `downvoteCount`

**File:** `src/CCE.Application/Community/Commands/VotePost/VotePostCommandHandler.cs`

```csharp
// Before
await _realtime.PublishToPostAsync(request.PostId, RealtimeEvents.VoteChanged,
    new { postId = request.PostId, post.UpvoteCount, post.Score }, cancellationToken)

// After
await _realtime.PublishToPostAsync(request.PostId, RealtimeEvents.VoteChanged,
    new { postId = request.PostId, post.UpvoteCount, post.DownvoteCount, post.Score }, cancellationToken)
```

**File:** `src/CCE.Application/Community/Commands/VoteReply/VoteReplyCommandHandler.cs`

```csharp
// Before
await _realtime.PublishToPostAsync(reply.PostId, RealtimeEvents.VoteChanged,
    new { replyId = reply.Id, reply.UpvoteCount, reply.Score }, cancellationToken)

// After
await _realtime.PublishToPostAsync(reply.PostId, RealtimeEvents.VoteChanged,
    new { replyId = reply.Id, reply.UpvoteCount, reply.DownvoteCount, reply.Score }, cancellationToken)
```

`PostReply` tracks both `UpvoteCount` and `DownvoteCount` (`PostReply.cs:35-36`); keeping the post and reply `VoteChanged` shapes symmetric avoids per-event-type sniffing on the client.

---

### 1.4 Fatten `PollResultsChanged` — eliminate refetch

The handler already has the full `poll` entity with all options in memory after `SaveChangesAsync`. No extra query.

**File:** `src/CCE.Application/Community/Commands/CastPollVote/CastPollVoteCommandHandler.cs`

```csharp
// Before
await _realtime.PublishToPostAsync(poll.PostId, RealtimeEvents.PollResultsChanged,
    new { pollId = poll.Id, poll.PostId }, cancellationToken);

// After
var totalVotes = poll.Options.Sum(o => o.VoteCount);
await _realtime.PublishToPostAsync(poll.PostId, RealtimeEvents.PollResultsChanged,
    new
    {
        pollId     = poll.Id,
        postId     = poll.PostId,
        totalVotes,
        options    = poll.Options
            .OrderBy(o => o.SortOrder)
            .Select(o => new
            {
                id         = o.Id,
                voteCount  = o.VoteCount,
                percentage = totalVotes == 0 ? 0d : Math.Round(o.VoteCount * 100d / totalVotes, 1),
            }),
    }, cancellationToken);
```

**Wire payload (inside envelope):**

```json
{
  "pollId": "...",
  "postId": "...",
  "totalVotes": 47,
  "options": [
    { "id": "...", "voteCount": 30, "percentage": 63.8 },
    { "id": "...", "voteCount": 17, "percentage": 36.2 }
  ]
}
```

Client maps directly onto existing poll UI — no GET /polls/{id}/results needed.

---

## Phase 2 — Payload fattening (do before mobile launch)

### 2.1 Fatten `NewReply` — eliminate refetch

The handler has the reply entity after save. Author display info requires one PK user lookup — the handler already injects `ICceDbContext`, so no new dependency.

**File:** `src/CCE.Application/Community/Commands/CreateReply/CreateReplyCommandHandler.cs`

Add after `await _uow.SaveChangesAsync(cancellationToken)`:

```csharp
// Single PK lookup — author is always the current user; this row is guaranteed to exist.
var author = await _db.Users.AsNoTracking()
    .Where(u => u.Id == reply.AuthorId)
    .Select(u => new { u.FirstName, u.LastName, u.AvatarUrl })
    .FirstOrDefaultAsync(cancellationToken)
    .ConfigureAwait(false);

await _realtime.PublishToPostAsync(post.Id, RealtimeEvents.NewReply,
    new
    {
        replyId      = reply.Id,
        postId       = post.Id,
        parentReplyId = reply.ParentReplyId,
        depth        = reply.Depth,
        body         = reply.Content,
        createdOn    = reply.CreatedOn,
        author = author is null ? null : new
        {
            id        = reply.AuthorId,
            name      = $"{author.FirstName} {author.LastName}".Trim(),
            avatarUrl = author.AvatarUrl,
        },
    }, cancellationToken).ConfigureAwait(false);
```

**Wire payload (inside envelope):**

```json
{
  "replyId": "...",
  "postId": "...",
  "parentReplyId": null,
  "depth": 0,
  "body": "Great point about the API design.",
  "createdOn": "2026-06-23T09:14:22.123Z",
  "author": {
    "id": "...",
    "name": "Sara Ahmed",
    "avatarUrl": "https://..."
  }
}
```

Mobile client inserts this node directly into the thread. No HTTP call. The `GET /posts/{id}/replies` endpoint remains as the fallback for initial load and deep subtree expansion.

---

### 2.2 Fatten `ReceiveNotification` — requires domain change

Currently `UserNotification` has no `actorId` (who triggered the notification) or `metaData` (context for constructing deep links). Without these, mobile can't build a tap target.

#### 2.2.a Domain entity change

**File:** `src/CCE.Domain/Notifications/UserNotification.cs` — add two fields:

```csharp
/// <summary>User who triggered this notification (nullable — system notifications have no actor).</summary>
public Guid? ActorId { get; private set; }

/// <summary>Key/value context for building deep links (e.g. postId, replyId, communityId).</summary>
public IReadOnlyDictionary<string, string> MetaData { get; private set; } = 
    System.Collections.Immutable.ImmutableDictionary<string, string>.Empty;
```

Update the `Render()` factory to accept `actorId` and `metaData` parameters. Add EF configuration (JSON column or a separate `notification_metadata` table — JSON column is simpler for this shape).

#### 2.2.b Payload change

**File:** `src/CCE.Infrastructure/Notifications/SignalRNotificationPublisher.cs`

```csharp
// After
await _hubContext.Clients.User(notification.UserId.ToString())
    .SendAsync(RealtimeEvents.ReceiveNotification,
        new
        {
            notification.Id,
            notification.TemplateId,
            notification.RenderedSubjectAr,
            notification.RenderedSubjectEn,
            notification.RenderedBody,
            notification.RenderedLocale,
            notification.Status,
            notification.SentOn,
            actorId  = notification.ActorId,       // ← new
            metaData = notification.MetaData,      // ← new: { "postId": "...", "replyId": "..." }
        },
        cancellationToken)
    .ConfigureAwait(false);
```

**Wire payload (inside envelope):**

```json
{
  "id": "...",
  "templateId": "COMMUNITY_MENTION",
  "renderedSubjectAr": "ذكرك سارة في تعليق",
  "renderedSubjectEn": "Sara mentioned you in a reply",
  "renderedBody": "...",
  "renderedLocale": "ar",
  "status": "Sent",
  "sentOn": "2026-06-23T09:14:22Z",
  "actorId": "uuid-of-sara",
  "metaData": { "postId": "...", "replyId": "..." }
}
```

Client: render toast from `renderedSubjectEn/Ar`, tap navigates to `/community/posts/{metaData.postId}#reply-{metaData.replyId}`. No HTTP call for the toast. Lazy-load full notification list when the bell panel opens.

---

## Phase 3 — Reconnect resilience

Mobile reconnects frequently. Without catch-up, a user coming back from background has stale vote counts, missing replies, and a stale poll.

### 3.1 Post-level sync endpoint

**New query:** `src/CCE.Application/Community/Public/Queries/GetPostActivity/GetPostActivityQuery.cs`

```csharp
public sealed record GetPostActivityQuery(
    System.Guid PostId,
    System.DateTimeOffset Since,
    System.Guid? UserId = null) : IRequest<Response<PostActivityDto>>;
```

**New DTO:** `PostActivityDto.cs`

```csharp
public sealed record PostActivityDto(
    int UpvoteCount,
    int DownvoteCount,
    int ReplyCount,
    int Score,
    System.Collections.Generic.IReadOnlyList<ReplyNodeDto> NewReplies,  // full nodes, same shape as NewReply payload
    PollSummaryDto? Poll);
```

**Handler logic (no Redis — reads from SQL directly):**

1. Fetch current post vote counts + reply count (one row by PK — fast).
2. Fetch new replies where `CreatedOn > since` (ordered, with author join).
3. Fetch poll via `PollHydrator.FetchAsync` if post is `PostType.Poll`.
4. Return assembled DTO.

**Endpoint registration** in `CommunityPublicEndpoints.cs`:

```csharp
community.MapGet("/posts/{id:guid}/activity", async (
    System.Guid id, System.DateTimeOffset since,
    ICurrentUserAccessor currentUser, IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(
        new GetPostActivityQuery(id, since, currentUser.GetUserId()), ct).ConfigureAwait(false);
    return result.ToHttpResult();
}).AllowAnonymous().WithName("GetPostActivity");
```

**Client reconnect flow:**

```
onreconnected:
  lastSeen = localStorage.getItem('lastEventTime')   // from envelope.occurredOn
  GET /api/community/posts/{activePostId}/activity?since={lastSeen}
  apply delta: patch vote counts, insert new reply nodes, update poll
  re-call Subscribe(activePostId) via hub
```

---

### 3.2 Feed-level sync endpoint (scope separately if time-constrained)

**New endpoint:** `GET /api/community/communities/{id}/feed/activity?since={timestamp}`

Returns:

```json
{
  "since": "2026-06-23T...",
  "newPostIds": ["uuid1", "uuid2"],
  "moderatedPostIds": ["uuid3"]
}
```

Client: show "2 new posts" banner; user taps to pull them. Remove tombstoned posts from the local list.

---

## Phase 4 — Mobile-specific hardening

### 4.1 Server-side typing debounce

Without throttling, a user who holds a key fires `StartTyping` on every keystroke. On a thread with 20 active participants, this saturates the WebSocket.

**New interface:** `src/CCE.Application/Common/Realtime/ITypingThrottle.cs`

```csharp
public interface ITypingThrottle
{
    /// <summary>Returns true if the typing event should be broadcast (not throttled).</summary>
    bool ShouldBroadcast(System.Guid postId, System.Guid userId);
}
```

**Implementation:** `src/CCE.Infrastructure/Notifications/MemoryCacheTypingThrottle.cs`

```csharp
public sealed class MemoryCacheTypingThrottle : ITypingThrottle
{
    private static readonly System.TimeSpan Window = System.TimeSpan.FromSeconds(2);
    private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;

    public MemoryCacheTypingThrottle(Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
        => _cache = cache;

    public bool ShouldBroadcast(System.Guid postId, System.Guid userId)
    {
        var key = $"typing:{postId}:{userId}";
        if (_cache.TryGetValue(key, out _)) return false;
        _cache.Set(key, true, Window);
        return true;
    }
}
```

Register as singleton (thread-safe within one process):

```csharp
services.AddSingleton<ITypingThrottle, MemoryCacheTypingThrottle>();
```

> **Multi-instance caveat:** `MemoryCache` is per-process. With the External + Internal APIs on separate hosts sharing the Redis backplane, each instance throttles independently — a single user could emit one `TypingChanged` per instance per 2 s window (i.e. up to 2× the budget across the fleet). Acceptable for an ephemeral UX signal. If stricter de-dup is ever needed, replace with a Redis `SETEX typing:{postId}:{userId} 2 NX` check in `ShouldBroadcast` (reuses the existing `IConnectionMultiplexer`).

**File:** `src/CCE.Infrastructure/Notifications/NotificationsHub.cs` — inject `ITypingThrottle` and apply:

```csharp
// In constructor — add ITypingThrottle throttle
_throttle = throttle;

// In BroadcastTypingAsync — guard before SendAsync
private Task BroadcastTypingAsync(System.Guid postId, bool isTyping)
{
    if (!System.Guid.TryParse(Context.UserIdentifier, out var userId))
        return Task.CompletedTask;

    // Only throttle "started typing" — always let "stopped" through so the indicator clears.
    if (isTyping && !_throttle.ShouldBroadcast(postId, userId))
        return Task.CompletedTask;

    return Clients.OthersInGroup(RealtimeGroups.Post(postId))
        .SendAsync(RealtimeEvents.TypingChanged, 
            new TypingChangedRealtime(postId, userId, isTyping));
}
```

---

### 4.2 Connection lifecycle guidance for clients

These are client-side responsibilities, documented here so the frontend team implements them correctly against the server we've built.

**Web (Angular):**

```typescript
// On tab hidden (visibilitychange):
if (document.hidden) {
  await connection.invoke('Unsubscribe', activePostId);   // triggers PresenceChanged for others
  // Do NOT stop() — tab may come back quickly. Hub keeps user:{id} room alive.
} else {
  await connection.invoke('Subscribe', activePostId);
  await this.runCatchUp();   // GET /activity?since=lastSeen
}
```

**Mobile (Flutter):**

```dart
// AppLifecycleState.paused → stop the connection entirely
await connection.stop();

// AppLifecycleState.resumed → reconnect + catch up
await connection.start();
await catchUpActivity(lastSeen);        // GET /activity?since=lastSeen
await connection.invoke('Subscribe', activePostId);
```

iOS and Android will kill a backgrounded WebSocket socket regardless — stopping it cleanly avoids the reconnect storm when resuming.

**Token refresh:**

The JWT is validated once at WebSocket upgrade only — the connection stays alive after expiry. Force a reconnect immediately after each token refresh so the next hub method invocations use claims from the new token:

```typescript
authService.onTokenRefreshed(() => {
  await connection.stop();
  await connection.start();
  // re-subscribe to active groups after start
});
```

---

## Implementation order and file checklist

### Phase 1 (before frontend writes any `connection.on`)

| # | File | Change |
|---|---|---|
| 1 | `CCE.Api.Common/SignalR/SignalRRegistration.cs` | Add `PropertyNamingPolicy = CamelCase` |
| 2 | `CCE.Application/Common/Realtime/RealtimePayloads.cs` | Add `RealtimeEnvelope` record with static `Wrap()` |
| 3 | `CCE.Infrastructure/Notifications/CommunityRealtimePublisher.cs` | Apply `Wrap()` in all 4 publish methods |
| 4 | `CCE.Infrastructure/Notifications/SignalRNotificationPublisher.cs` | Wrap `ReceiveNotification` push (covers `user:{id}`) |
| 5 | `CCE.Infrastructure/Notifications/NotificationsHub.cs` | Wrap `PresenceChanged` + `TypingChanged` broadcasts |
| 6 | `CCE.Application/Common/Messaging/IntegrationEvents/PostCreatedIntegrationEvent.cs` | Add `Title` field to the record |
| 7 | `CCE.Application/Community/EventHandlers/PostCreatedBusPublisher.cs` | Pass `post.Title` when constructing the event |
| 8 | `CCE.Infrastructure/Notifications/Messaging/Consumers/SignalRConsumer.cs` | Wrap `NewPost` push; include `Title` |
| 9 | `CCE.Application/Community/Commands/VotePost/VotePostCommandHandler.cs` | Add `DownvoteCount` to `VoteChanged` |
| 10 | `CCE.Application/Community/Commands/VoteReply/VoteReplyCommandHandler.cs` | Add `DownvoteCount` to reply `VoteChanged` — keeps post/reply payloads symmetric |
| 11 | `CCE.Application/Community/Commands/CastPollVote/CastPollVoteCommandHandler.cs` | Fatten `PollResultsChanged` with options |

### Phase 2 (before mobile launch)

| # | File | Change |
|---|---|---|
| 12 | `CCE.Application/Community/Commands/CreateReply/CreateReplyCommandHandler.cs` | Fatten `NewReply` with author + `Content` (NOTE: field is `Content`, not `Body`) |
| 13 | `CCE.Domain/Notifications/UserNotification.cs` | Add `ActorId`, `MetaData` properties |
| 14 | `CCE.Infrastructure/Persistence/Configurations/Identity/UserNotificationConfiguration.cs` | EF config for new fields (JSON column) |
| 15 | `CCE.Infrastructure/Notifications/SignalRNotificationPublisher.cs` | Add `actorId`, `metaData` to push (already wrapped by Phase 1 item 4) |
| 16 | EF migration | Add columns, snapshot |

### Phase 3 (before beta)

| # | File | Change |
|---|---|---|
| 17 | `CCE.Application/Community/Public/Queries/GetPostActivity/GetPostActivityQuery.cs` | New query record |
| 18 | `CCE.Application/Community/Public/Queries/GetPostActivity/GetPostActivityQueryHandler.cs` | Handler |
| 19 | `CCE.Application/Community/Public/Dtos/PostActivityDto.cs` | New DTO |
| 20 | `CCE.Api.External/Endpoints/CommunityPublicEndpoints.cs` | Register endpoint |

### Phase 4 (before GA)

| # | File | Change |
|---|---|---|
| 21 | `CCE.Application/Common/Realtime/ITypingThrottle.cs` | New interface |
| 22 | `CCE.Infrastructure/Notifications/MemoryCacheTypingThrottle.cs` | Implementation |
| 23 | `CCE.Infrastructure/Notifications/NotificationsHub.cs` | Inject throttle, apply in `BroadcastTypingAsync` |
| 24 | `CCE.Infrastructure/DependencyInjection.cs` | Register `ITypingThrottle` as singleton |

---

## What does NOT change

- Hub path stays `/hubs/notifications` on **both** APIs (External port 5001, Internal port 5002). The two share the same Redis backplane so a publish on either reaches clients on both — see the Option 2 decision ("Add hub to Internal API") in `signalr-rooms.md`. Each API validates its own JWT scheme (`LocalAuthApi.External` vs `LocalAuthApi.Internal`); both use the shared `SubClaimUserIdProvider` for `user:{id}` group routing.  
- Group names (`user:`, `post:`, `community:`, `topic:`, `moderation`) are stable.  
- Hub subscription methods (`Subscribe`, `Unsubscribe`, `SubscribeCommunity`, etc.) do not change.  
- `NewPost` stays as a toast trigger only — full feed card rendering always requires a GET.  
- Poll data is never cached in Redis — `PollHydrator` always reads fresh SQL. The fattened `PollResultsChanged` push is the only realtime path; no Redis consumer needed.
