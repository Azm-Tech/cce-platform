# Community Redis Bug-Fix Implementation Plan

Covers all 10 issues from the system rating. Ordered by severity: critical first, then high, medium, low.
Each issue lists the exact files to touch and the exact change required.

---

## Phase 1 — Critical Fixes (3 issues)

---

### Fix 1 — Soft-delete does not clean Redis

**Root cause:**
`SoftDeletePostCommandHandler` calls `post.SoftDelete(...)` and saves, but never removes the post from
`feed:community:{id}`, `feed:user:{*}`, or `hot:{communityId}`. The interface already has both removal
methods — they are just not called. The post stays in Redis until TTL, and because `HydrateAsync` silently
drops deleted IDs, every page between now and TTL returns fewer items than `pageSize` while `total` stays
inflated. Pagination is broken for every moderation action.

**Files to change:**

`src/CCE.Application/Community/IRedisFeedStore.cs`
- Add a new method:
```csharp
/// Removes postId from feed:community:{communityId}, hot:{communityId},
/// and optionally from a specific user's feed:user:{userId}.
Task RemovePostFromAllFeedsAsync(Guid communityId, Guid postId, CancellationToken ct = default);
```

`src/CCE.Infrastructure/Community/RedisFeedStore.cs`
- Implement `RemovePostFromAllFeedsAsync`:
```csharp
public async Task RemovePostFromAllFeedsAsync(Guid communityId, Guid postId, CancellationToken ct = default)
{
    try
    {
        var db = Db;
        await db.SortedSetRemoveAsync($"feed:community:{communityId}", postId.ToString()).ConfigureAwait(false);
        await db.SortedSetRemoveAsync($"hot:{communityId}", postId.ToString()).ConfigureAwait(false);
    }
    catch (RedisException ex)
    {
        _logger.LogWarning(ex, "Redis unavailable for RemovePostFromAllFeedsAsync(community={CommunityId}, post={PostId}).", communityId, postId);
    }
}
```
> Note: personal `feed:user:{*}` cannot be cleaned without a reverse index (see Fix 4 for the full
> discussion). For now, removing from the community timeline and hot leaderboard is sufficient — these
> are the two keys whose cardinality is used as the pagination total. Personal feeds still self-heal at
> 24h TTL or when `HydrateAsync` drops the stale ID.

`src/CCE.Application/Community/Commands/SoftDeletePost/SoftDeletePostCommandHandler.cs`
- Inject `IRedisFeedStore _feedStore`
- After `await _service.UpdatePostAsync(post, ...)`, add:
```csharp
if (wasPublished)
{
    await _feedStore.RemovePostFromAllFeedsAsync(post.CommunityId, post.Id, cancellationToken)
        .ConfigureAwait(false);
}
```

**Test:**
1. Publish a post. Verify it appears in the community feed.
2. Soft-delete the post. Call `GET /api/community/feed?communityId={id}&sort=Newest`.
3. Assert the deleted post is not in results and `total` has decremented by 1.

---

### Fix 2 — VoteConsumer is not idempotent

**Root cause:**
`VoteConsumer` uses `HashIncrementAsync` (Redis `HINCRBY`). `VoteConsumerDefinition` configures retries
at `200ms / 500ms / 1000ms` with `ConcurrentMessageLimit = 50`. If any of those 50 parallel consumers
write to Redis and then crash before acknowledging, MassTransit redelivers the message and the counter
increments again permanently. `post:{postId}:meta` can never self-heal without a full admin rebuild — and
there is no admin rebuild endpoint for it (only for `hot:{communityId}`).

**Strategy:** replace `HINCRBY` with `SetPostMetaAsync` (absolute set from the event's authoritative
counts). `VoteCreatedIntegrationEvent` already carries `UpvoteCount`, `DownvoteCount`, and `Score` from
the domain aggregate — these are the SQL-committed values. Writing them absolutely makes the consumer
fully idempotent: replaying the message sets the same values, not different ones.

**Files to change:**

`src/CCE.Infrastructure/Notifications/Messaging/Consumers/VoteConsumer.cs`
- Replace `IncrementPostVotesAsync` with `SetPostMetaAsync`:
```csharp
public async Task Consume(ConsumeContext<VoteCreatedIntegrationEvent> context)
{
    var evt = context.Message;

    // Idempotent absolute write — safe to replay on retry.
    // Uses the authoritative counts from the domain aggregate (already committed to SQL).
    await _feedStore.SetPostMetaAsync(
        evt.PostId,
        evt.UpvoteCount,
        evt.DownvoteCount,
        evt.Score,
        replyCount: 0,          // reply count not carried on vote events; preserve existing value
        context.CancellationToken)
        .ConfigureAwait(false);

    await _feedStore.AddToHotLeaderboardAsync(
        evt.CommunityId, evt.PostId, evt.Score, context.CancellationToken)
        .ConfigureAwait(false);
}
```

**Caveat on replyCount:**
`SetPostMetaAsync` overwrites all four fields including `replyCount`. Until reply events also carry reply
count in their integration event, pass `replyCount: 0` and accept that the reply counter in the hash
resets on each vote. The display layer should always prefer the SQL `CommentsCount` field over the Redis
hash value for reply counts. If the hash is used for reply display, either:
- (a) read the existing replyCount from the hash first and pass it through, or
- (b) split `SetPostMetaAsync` into separate methods per field.

Option (a) is simplest: add a `GetPostMetaAsync` call before `SetPostMetaAsync` to preserve the existing
`replyCount`.

**Files to change (option a, preferred):**
```csharp
var existing = await _feedStore.GetPostMetaAsync(evt.PostId, context.CancellationToken)
    .ConfigureAwait(false);
await _feedStore.SetPostMetaAsync(
    evt.PostId,
    evt.UpvoteCount,
    evt.DownvoteCount,
    evt.Score,
    replyCount: existing?.ReplyCount ?? 0,
    context.CancellationToken)
    .ConfigureAwait(false);
```

**Test:**
1. Publish a post, cast 3 upvotes (different users).
2. Manually replay `VoteCreatedIntegrationEvent` three times with the same payload.
3. Assert `post:{postId}:meta` hash shows `upvotes = 3`, not `upvotes = 9`.

---

### Fix 3 — Hot feed pagination breaks past page 50 (pageSize=20)

**Root cause:**
`GetHotPostsAsync(communityId, int topN)` uses `ZREVRANGEBYRANK 0 topN-1` then the query handler
does `.Skip((page-1)*pageSize).Take(pageSize)` in memory. The leaderboard is capped at 1000 entries.
With `pageSize=20`, requesting page 51 calls `GetHotPostsAsync(communityId, 1020)` — Redis clamps
at 1000 and returns 1000 entries. The in-memory skip of 1000 leaves 0 items. Users on page 51+ always
see empty results. Also, fetching 1000 entries over the network to serve 20 is wasteful.

`GetCommunityFeedAsync` already does this correctly by accepting `page` and `pageSize` and passing
offset/count directly to `SortedSetRangeByRankAsync`. `GetHotPostsAsync` needs the same treatment.

**Files to change:**

`src/CCE.Application/Community/IRedisFeedStore.cs`
- Change signature (breaking change — only one call site in the query handler):
```csharp
// Before:
Task<IReadOnlyList<Guid>> GetHotPostsAsync(Guid communityId, int topN, CancellationToken ct = default);

// After:
Task<IReadOnlyList<Guid>> GetHotPostsAsync(Guid communityId, int page, int pageSize, CancellationToken ct = default);
```

`src/CCE.Infrastructure/Community/RedisFeedStore.cs`
- Update implementation:
```csharp
public async Task<IReadOnlyList<Guid>> GetHotPostsAsync(
    Guid communityId, int page, int pageSize, CancellationToken ct = default)
{
    try
    {
        var start = (page - 1) * pageSize;
        var stop  = start + pageSize - 1;
        var entries = await Db
            .SortedSetRangeByRankAsync($"hot:{communityId}", start, stop, Order.Descending)
            .ConfigureAwait(false);
        return entries.Select(e => Guid.Parse(e.ToString())).ToList();
    }
    catch (RedisException ex)
    {
        _logger.LogWarning(ex, "Redis unavailable for GetHotPostsAsync(community={CommunityId}).", communityId);
        return Array.Empty<Guid>();
    }
}
```

`src/CCE.Application/Community/Public/Queries/ListCommunityFeed/ListCommunityFeedQueryHandler.cs`
- Update the call site (remove the manual Skip/Take):
```csharp
// Before:
var ids = request.Sort == PostFeedSort.Hot
    ? (await _feedStore.GetHotPostsAsync(communityId, page * pageSize, cancellationToken).ConfigureAwait(false))
        .Skip((page - 1) * pageSize).Take(pageSize).ToList()
    : ...

// After:
var ids = request.Sort == PostFeedSort.Hot
    ? (await _feedStore.GetHotPostsAsync(communityId, page, pageSize, cancellationToken).ConfigureAwait(false))
        .ToList()
    : ...
```

Also update `RebuildHotLeaderboardCommandHandler` — it does not call `GetHotPostsAsync` so no change needed there. Check any test harnesses that call `GetHotPostsAsync` with the old signature.

**Test:**
1. Seed a community with 120 published posts with distinct scores.
2. Call hot feed page 1, 2, 3, 4, 5, 6 (pageSize=20) — assert each returns 20 distinct posts.
3. Assert no post appears on two different pages.

---

## Phase 2 — High Fixes (2 issues)

---

### Fix 4 — Unfollow / leave community does not purge personal feed

**Root cause:**
`SetCommunityFollowCommandHandler` (unfollow path) and `LeaveCommunityCommandHandler` remove the SQL row
but never touch `feed:user:{userId}`. The user's personal feed retains that community's posts for 24h.

**Constraint:** cleaning personal feeds on unfollow requires knowing which post IDs in
`feed:user:{userId}` belong to the unfollowed community/topic. Redis sorted-sets do not support
filtering by metadata — only by score or member value. Options:

- **Option A (recommended):** Add a reverse index `community:{communityId}:posts` as a Redis set.
  FeedConsumer writes to it on publish. On unfollow, load the set and call `ZREM` for each member.
  TTL matches `feed:community:{communityId}` (24h). If the reverse index is cold, fall back gracefully
  (do nothing — the 24h TTL will self-heal).

- **Option B (pragmatic short-term):** Accept the stale window. `HydrateAsync` already guards with
  `community.IsActive && community.Visibility == Public`. Add a membership guard:
  `_db.CommunityMembers.Any(m => m.UserId == userId && m.CommunityId == p.CommunityId)` in the personal
  feed hydration. This fixes visibility correctness without Redis cleanup.

**Recommended path: Option B now, Option A later when personal feed volume justifies it.**

`src/CCE.Application/Community/Commands/SetCommunityFollow/SetCommunityFollowCommandHandler.cs`
- No Redis change needed for Option B.

`src/CCE.Application/Community/Public/Queries/ListUserFeed/ListUserFeedQueryHandler.cs`
(when the personal feed query exists — add the membership guard to HydrateAsync):
```csharp
.Where(p => _db.CommunityMembers.Any(m =>
    m.UserId == userId && m.CommunityId == p.CommunityId))
```

For `LeaveCommunityCommandHandler`, document that the feed self-heals at 24h TTL. Add a log line so
it is observable.

**If Option A is chosen later:**

`src/CCE.Application/Community/IRedisFeedStore.cs` — add:
```csharp
Task AddPostToCommunityPostsIndexAsync(Guid communityId, Guid postId, CancellationToken ct = default);
Task<IReadOnlyList<Guid>> GetCommunityPostIdsAsync(Guid communityId, CancellationToken ct = default);
```

`src/CCE.Infrastructure/Notifications/Messaging/Consumers/FeedConsumer.cs`
- After `AddToCommunityFeedAsync`, also call `AddPostToCommunityPostsIndexAsync`.

`src/CCE.Application/Community/Commands/SetCommunityFollow/SetCommunityFollowCommandHandler.cs`
- On unfollow path, load the reverse index and call `RemoveFromFeedAsync` per post ID.

---

### Fix 5 — FeedConsumer: N+1 Redis writes and 5 sequential SQL queries

**Root cause (SQL):**
Three follower queries (`UserFollows`, `CommunityFollows`, `TopicFollows`) are awaited sequentially.
They are fully independent and can run in parallel. For a post with 3,000 combined followers, the
consumer currently spends ~3× the single-query latency before any Redis work starts.

**Root cause (Redis):**
Each `AddToUserFeedAsync` call does two Redis round trips (`ZADD` + `EXPIRE`). For 5,000 followers
= 10,000 round trips. StackExchange.Redis supports `IBatch` (fire-and-forget pipeline) and
`ITransaction` for pipelining. `IBatch` is the right tool here.

**Files to change:**

`src/CCE.Infrastructure/Notifications/Messaging/Consumers/FeedConsumer.cs`
- Parallelize the three follower SQL queries:
```csharp
var (userFollowerTask, communityFollowerTask, topicFollowerTask) = (
    _db.UserFollows.AsNoTracking()
        .Where(f => f.FollowedId == evt.AuthorId)
        .Select(f => f.FollowerId)
        .ToListAsync(context.CancellationToken),
    _db.CommunityFollows.AsNoTracking()
        .Where(f => f.CommunityId == evt.CommunityId)
        .Select(f => f.UserId)
        .ToListAsync(context.CancellationToken),
    _db.TopicFollows.AsNoTracking()
        .Where(f => f.TopicId == evt.TopicId)
        .Select(f => f.UserId)
        .ToListAsync(context.CancellationToken)
);
await Task.WhenAll(userFollowerTask, communityFollowerTask, topicFollowerTask).ConfigureAwait(false);
followerIds.UnionWith(userFollowerTask.Result);
followerIds.UnionWith(communityFollowerTask.Result);
followerIds.UnionWith(topicFollowerTask.Result);
```

`src/CCE.Application/Community/IRedisFeedStore.cs` — add batch method:
```csharp
Task AddToUserFeedBatchAsync(IReadOnlyCollection<Guid> userIds, Guid postId,
    DateTimeOffset publishedOn, CancellationToken ct = default);
```

`src/CCE.Infrastructure/Community/RedisFeedStore.cs` — implement using `IBatch`:
```csharp
public async Task AddToUserFeedBatchAsync(IReadOnlyCollection<Guid> userIds, Guid postId,
    DateTimeOffset publishedOn, CancellationToken ct = default)
{
    if (userIds.Count == 0) return;
    try
    {
        var db = Db;
        var score = publishedOn.ToUnixTimeSeconds();
        var member = postId.ToString();
        var batch = db.CreateBatch();
        var tasks = new List<Task>(userIds.Count * 2);
        foreach (var userId in userIds)
        {
            var key = $"feed:user:{userId}";
            tasks.Add(batch.SortedSetAddAsync(key, member, score));
            tasks.Add(batch.KeyExpireAsync(key, FeedTtl));
        }
        batch.Execute();
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
    catch (RedisException ex)
    {
        _logger.LogWarning(ex, "Redis unavailable for AddToUserFeedBatchAsync (post={PostId}, users={Count}).",
            postId, userIds.Count);
    }
}
```

`src/CCE.Infrastructure/Notifications/Messaging/Consumers/FeedConsumer.cs`
- Replace the `foreach` fan-out loop with:
```csharp
await _feedStore.AddToUserFeedBatchAsync(followerIds, evt.PostId, evt.PublishedOn, context.CancellationToken)
    .ConfigureAwait(false);
```

---

## Phase 3 — Medium Fixes (3 issues)

---

### Fix 6 — `IsExpert: false` always wrong in `PostCreatedIntegrationEvent`

**Root cause:**
`PostCreatedBusPublisher` hardcodes `IsExpert: false`. FeedConsumer immediately re-queries
`ExpertProfiles` to get the real value, so the field in the event is never used correctly.
Any future consumer that reads `evt.IsExpert` and trusts it will get wrong behavior silently.

**Options:**
- **Option A:** Resolve `IsExpert` before publishing (in `PostCreatedBusPublisher`) by querying
  `_db.ExpertProfiles.AnyAsync(e => e.UserId == notification.AuthorId)`. Cost: one SQL query per
  publish, synchronous in the domain event handler. Acceptable.
- **Option B:** Remove `IsExpert` from the event entirely. FeedConsumer resolves it from SQL.
  Cleaner — the event is a fact ("post was created"), not a derived state snapshot.

**Recommended: Option B.**

`src/CCE.Application/Common/Messaging/IntegrationEvents/PostCreatedIntegrationEvent.cs`
- Remove `bool IsExpert` parameter.

`src/CCE.Application/Community/EventHandlers/PostCreatedBusPublisher.cs`
- Remove the `IsExpert: false` argument from the constructor call.

`src/CCE.Infrastructure/Notifications/Messaging/Consumers/FeedConsumer.cs`
- Remove `evt.IsExpert ||` from the expert check (FeedConsumer already does the SQL lookup).
  Before: `var isExpert = evt.IsExpert || await _db.ExpertProfiles.AnyAsync(...)`
  After:  `var isExpert = await _db.ExpertProfiles.AnyAsync(...)`

Check any test that constructs `PostCreatedIntegrationEvent` — update the constructor call.

---

### Fix 7 — Celebrity threshold `10_000` is a magic number

**Root cause:**
`author?.FollowerCount > 10_000` in FeedConsumer. Changing it requires a code deploy.

**Files to change:**

`src/CCE.Infrastructure/DependencyInjection.cs` or the Infrastructure options class:
Add `CelebrityFollowerThreshold` to `CceInfrastructureOptions` (or a dedicated `CommunityOptions`):
```csharp
public int CelebrityFollowerThreshold { get; set; } = 10_000;
```

`appsettings.json` (both APIs + Worker):
```json
"Community": {
  "CelebrityFollowerThreshold": 10000
}
```

`src/CCE.Infrastructure/Notifications/Messaging/Consumers/FeedConsumer.cs`
- Inject `IOptions<CommunityOptions>` and replace the literal:
```csharp
var isCelebrity = isExpert || (author?.FollowerCount > _opts.CelebrityFollowerThreshold);
```

---

### Fix 8 — `notif:{userId}:count` can only reset to 0, never decrement by 1

**Root cause:**
`ResetNotificationCountAsync` deletes the key. There is no call site that passes `delta = -1` to
`IncrementNotificationCountAsync` when a single notification is marked as read. Badge count is
all-or-nothing.

**Files to change:**

Find the "mark notification as read" command handler (likely `MarkNotificationReadCommandHandler`).
After marking as read in SQL, call:
```csharp
await _feedStore.IncrementNotificationCountAsync(userId, delta: -1, cancellationToken)
    .ConfigureAwait(false);
```

`src/CCE.Infrastructure/Community/RedisFeedStore.cs` — guard against negative counts:
```csharp
public async Task IncrementNotificationCountAsync(Guid userId, int delta = 1, CancellationToken ct = default)
{
    try
    {
        var key = $"notif:{userId}:count";
        var newVal = await Db.StringIncrementAsync(key, delta).ConfigureAwait(false);
        if (newVal < 0)
            await Db.KeyDeleteAsync(key).ConfigureAwait(false);  // clamp to 0
        else
            await Db.KeyExpireAsync(key, NotifTtl).ConfigureAwait(false);
    }
    catch (RedisException ex) { ... }
}
```

---

## Phase 4 — Low Fixes (2 issues)

---

### Fix 9 — `RemoveFromFeedAsync` only removes from personal feeds

**Root cause:**
The existing `RemoveFromFeedAsync(Guid userId, Guid postId)` only targets `feed:user:{userId}`.
There is no method to remove a post from `feed:community:{communityId}`. Fix 1 adds
`RemovePostFromAllFeedsAsync` which fills this gap for the community timeline and hot leaderboard.

After Fix 1 is in place, rename `RemoveFromFeedAsync` to `RemoveFromUserFeedAsync` to make the
distinction explicit:

`src/CCE.Application/Community/IRedisFeedStore.cs`
```csharp
// Rename for clarity:
Task RemoveFromUserFeedAsync(Guid userId, Guid postId, CancellationToken ct = default);
```

Update the single call site (if any) and the implementation in `RedisFeedStore.cs`.

---

### Fix 10 — `PostCreatedIntegrationEvent.Locale` is unused

**Root cause:**
`Locale` is in the event but neither FeedConsumer, SignalRConsumer, nor NotificationConsumer uses it.
It adds payload weight with no effect.

**Options:**
- Remove it from the event (breaking — check all consumers and test harnesses).
- Keep it if future localization of notifications is planned (document this intent).

**Recommended:** keep it but add an XML doc comment explaining its intended use. Do not remove unless
it is confirmed that no future consumer will need it, to avoid adding it back later.

`src/CCE.Application/Common/Messaging/IntegrationEvents/PostCreatedIntegrationEvent.cs`
```csharp
/// <summary>
/// BCP-47 locale of the post content (e.g. "ar", "en").
/// Reserved for future use by localized notification consumers — not currently read.
/// </summary>
string Locale
```

---

## Sequencing Summary

| Phase | Fix | Effort | Risk |
|---|---|---|---|
| 1 | Fix 1 — Soft-delete cleans Redis | Small | Low |
| 1 | Fix 2 — VoteConsumer idempotent | Small | Low |
| 1 | Fix 3 — Hot feed pagination | Small | Low |
| 2 | Fix 4 — Unfollow feed cleanup (Option B) | Small | Low |
| 2 | Fix 5 — Fan-out batching + SQL parallelism | Medium | Low |
| 3 | Fix 6 — Remove `IsExpert` from event | Small | Low |
| 3 | Fix 7 — Celebrity threshold to config | Small | Low |
| 3 | Fix 8 — Notification count decrement | Small | Low |
| 4 | Fix 9 — Rename `RemoveFromFeedAsync` | Trivial | Low |
| 4 | Fix 10 — Document `Locale` field | Trivial | None |

All fixes are independent. They can be batched into two PRs:
- **PR 1:** Phase 1 (3 critical fixes) — ship first.
- **PR 2:** Phase 2–4 (remaining 7) — ship together or incrementally.

No migration is required. No new tables. No API contract changes.
The only breaking change is the `GetHotPostsAsync` signature (Fix 3) — internal to the Application
+ Infrastructure boundary, no external API impact.
