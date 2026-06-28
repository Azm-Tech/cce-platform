# Feed Cache, Redis & Interest Algorithm Review

**Date:** 2026-06-16  
**Branch:** `feat/add-content-interest-topic-links`  
**Reviewer:** Claude Code

---

## Summary

| Severity | Count |
|----------|-------|
| 🔴 Critical | 1 |
| 🟠 High     | 2 |
| 🟡 Medium   | 3 |
| 🔵 Low      | 2 |

---

## Part 1 — Community Feed, Cache & Redis

### 🔴 BUG-1 (Critical): Hot Leaderboard Trim Destroys Entries for Small Communities

**File:** `src/CCE.Infrastructure/Community/RedisFeedStore.cs:200–201`

```csharp
await Db.SortedSetAddAsync(key, postId.ToString(), score).ConfigureAwait(false);
await Db.SortedSetRemoveRangeByRankAsync(key, 0, -1001).ConfigureAwait(false); // trim to top 1000
```

`ZREMRANGEBYRANK key 0 -1001` is only safe when the set already has **> 1000 members**. When it has N ≤ 1000, Redis resolves rank `-1001` as `max(0, N − 1001)`, which clamps to **0**. The command becomes `ZREMRANGEBYRANK key 0 0`, which removes the **just-inserted lowest-scored entry** on every call.

**Observable impact:** In a community with fewer than 1001 posts, every `AddToHotLeaderboardAsync` call adds one entry and immediately removes the lowest-scored one. The leaderboard never grows beyond the count at which the first trim fired. For a fresh community the leaderboard perpetually stays empty (add → trim to 0 → add → trim to 0 …).

**Fix:** trim only after the threshold is exceeded:

```csharp
await Db.SortedSetAddAsync(key, postId.ToString(), score).ConfigureAwait(false);
// Only trim when we exceed 1 000 — rank 0 is safest to express as a length check.
var len = await Db.SortedSetLengthAsync(key).ConfigureAwait(false);
if (len > 1000)
    await Db.SortedSetRemoveRangeByRankAsync(key, 0, (long)(len - 1001)).ConfigureAwait(false);
await Db.KeyExpireAsync(key, HotTtl).ConfigureAwait(false);
```

---

### 🟠 BUG-2 (High): VoteConsumer Delta Wrong on Last-Vote Retraction

**File:** `src/CCE.Infrastructure/Notifications/Messaging/Consumers/VoteConsumer.cs:36–37`

```csharp
var upDelta   = evt.Direction == 1  ? 1  : evt.Direction == -1 ? 0  : evt.UpvoteCount   > 0 ? -1 : 0;
var downDelta = evt.Direction == -1 ? 1  : evt.Direction ==  1 ? 0  : evt.DownvoteCount > 0 ? -1 : 0;
```

`Direction == 0` means the user **retracted** their vote. `evt.UpvoteCount` carries the **post-retraction** SQL count. When the user removes the last upvote, SQL has already decremented to 0, so `UpvoteCount == 0` → `upDelta = 0`. Redis is never decremented: its counter diverges permanently from SQL (`+1` phantom upvote).

**Example:**
| Step | SQL UpvoteCount | Redis UpvoteCount |
|------|-----------------|-------------------|
| Initial | 1 | 1 |
| User retracts (Direction=0, evt.UpvoteCount=0) | 0 | **1** (not decremented) |

The same defect applies to the last downvote.

**Fix:** The event needs to carry the **previous** direction so the consumer knows what was removed, or the direction-0 branch should always decrement by 1 for whichever counter was previously non-zero. The cleanest fix is to add `PreviousDirection int` to `VoteCreatedIntegrationEvent` and use it here.

---

### 🟠 BUG-3 (High): Hot Leaderboard Score Never Updated After Votes

**Files:** `VoteConsumer.cs`, `RankingConsumer.cs`

`VoteConsumer` updates `post:{postId}:meta` hash counters but never touches `hot:{communityId}` sorted-set score. `RankingConsumer` rebuilds the hot leaderboard — but only on `PostCreatedIntegrationEvent`. In a community that stops publishing new posts, vote changes never propagate to the hot leaderboard: a heavily downvoted post keeps a high rank indefinitely and a suddenly-popular post never rises.

**Fix:** `VoteConsumer` should call `_feedStore.AddToHotLeaderboardAsync(evt.CommunityId, evt.PostId, evt.Score, ct)` to push the updated score. `IRedisFeedStore.AddToHotLeaderboardAsync` already accepts a `score` parameter; the consumer just needs to call it.

---

### 🟡 ISSUE-4 (Medium): Stale Redis IDs Cause Phantom Pagination

**File:** `src/CCE.Application/Community/Public/Queries/ListCommunityFeed/ListCommunityFeedQueryHandler.cs:56–62`

When the Redis fast-path is taken, `total` is fetched from SQL (`CountAsync` on published posts) while the actual items come from hydrating Redis IDs. `HydrateAsync` silently drops IDs for posts that were deleted or unpublished after fan-out (there is no cleanup consumer that removes IDs from `feed:community:{id}` or `hot:{id}` on post deletion). 

**Result:** The client receives `total = 200` but page 1 shows only 12 of 20 requested items (8 stale IDs were silently dropped), causing broken pagination: pages appear shorter than `pageSize` even though `total` claims content remains.

**Fix:** Either add a `PostDeletedConsumer` that calls `_feedStore.RemoveFromHotLeaderboardAsync` / `RemoveFromFeedAsync`, **or** base `total` on the Redis sorted-set length rather than SQL when the Redis path is taken.

---

### 🟡 ISSUE-5 (Medium): Output Cache Not Invalidated After Async Fan-Out

**Files:** `RedisOutputCacheMiddleware.cs`, `FeedConsumer.cs`, `VoteConsumer.cs`

`CacheInvalidationBehavior` is the only invalidation path: it runs synchronously after a command succeeds, within the same request. The MassTransit consumers (`FeedConsumer`, `VoteConsumer`, `RankingConsumer`) run in a separate process/thread after the message is dequeued from the outbox. They update Redis sorted-sets and post metadata but have no connection to `IOutputCacheInvalidator`.

**Impact:** Anonymous requests to `/api/community/*` (region `Posts`) and `/api/feed/*` (region `Feed`) are served from the output cache. After `FeedConsumer` adds a new post to `feed:community:{id}`, the cached HTTP response for that route still shows the old list until the TTL expires. Only authenticated users (who bypass the cache via `HasAuth`) see fresh data immediately.

**Fix:** Inject `IOutputCacheInvalidator` into `FeedConsumer` and evict `CacheRegions.Posts` (and optionally `CacheRegions.Feed`) after a successful fan-out.

---

### 🔵 ISSUE-6 (Low): FeedConsumer Fan-Out Loop Is Unbounded

**File:** `FeedConsumer.cs:96–101`

```csharp
foreach (var userId in followerIds)
{
    await _feedStore.AddToUserFeedAsync(userId, evt.PostId, ...);
}
```

Fan-out issues one sequential Redis write per follower. A non-celebrity author with 9,999 followers (just under the celebrity threshold) produces 9,999 sequential round-trips to Redis inside a single consumer message. Under burst load this blocks the consumer for seconds and may hit MassTransit's message-lock timeout.

**Fix:** Use a Redis pipeline (`IBatch`) or fan the writes in parallel chunks (`Parallel.ForEachAsync` with a concurrency cap, e.g., 64).

---

## Part 2 — User Interest / Personalization Algorithm

### Algorithm overview

`UserContentInterestResolver.ResolveAsync` looks up the user's stored `knowledge_assessment` and `job_sector` interest topics and fills in any unspecified explicit filter params. `ListPublicNewsQueryHandler` then filters and ranks content with a 0–3 point binary-match score:

| Points | Meaning |
|--------|---------|
| 3 | knowledge-level AND job-sector match |
| 2 | knowledge-level match only |
| 1 | job-sector match only |
| 0 | generic (no tags) |

Content with a **different** knowledge level or job sector than the user is excluded entirely (not demoted). This is a coherent, lightweight relevance design for a non-ML context.

### What works well

- The resolver falls back gracefully: if the user is anonymous, or has no stored interest, it returns the explicit params as-is (no crash, no empty result).
- Explicit params passed in the request always take priority (`HasValue && HasValue` early return).
- Generic content (`null` tags) is never excluded — it always appears as a fallback at the bottom of the ranked list.
- The resolver is used consistently across news, resources, and events query handlers.

---

### 🟡 BUG-7 (Medium): CarbonArea Interests Collected but Never Applied

**File:** `src/CCE.Application/Content/UserContentInterestResolver.cs`

`UserInterestTopic` stores three categories: `knowledge_assessment`, `job_sector`, and `carbon_area` (multi-select). The resolver only reads the first two. Carbon area IDs are silently ignored during content filtering and ranking.

**Impact:** Users who invest time in the carbon-area onboarding step receive zero benefit — no content is prioritised by their chosen carbon areas. The `carbon_area` column in `news`, `resources`, and `events` tables (if it exists) is dead weight from the user's perspective.

**Fix:** Either (a) add `CarbonAreaIds` to the resolver output and use them in content WHERE clauses, or (b) remove the carbon area step from onboarding and the `UserInterestTopics` write-path until the feature is fully wired.

---

### 🔵 ISSUE-8 (Low): No Interest-Based Ranking in Community Feed

**File:** `ListCommunityFeedQueryHandler.cs`

`IUserContentInterestResolver` is not used in the community feed handler. All users see posts in the same Hot / Newest / TopVoted order regardless of their knowledge level or job sector. If posts carry `KnowledgeLevelId` or `JobSectorId` tags (same as news items), these are never used for personalised ranking.

This is likely a conscious design decision (community feeds are social / community-scoped, not content-editorial), but it creates an inconsistency: news is personalised by interest, community posts are not.

If interest-based boosting is desired in the community feed, the SQL path can apply the same 0–3 scoring after calling `_resolver.ResolveAsync`. The Redis fast-path (fan-out sorted-sets) cannot be personalised cheaply without per-user sorted-sets, which the fan-out already maintains for the Newest case.

---

## Appendix — Files Reviewed

| File | Area |
|------|------|
| `src/CCE.Application/Community/Public/Queries/ListCommunityFeed/ListCommunityFeedQueryHandler.cs` | Feed read |
| `src/CCE.Infrastructure/Community/RedisFeedStore.cs` | Redis store impl |
| `src/CCE.Application/Community/IRedisFeedStore.cs` | Redis store interface |
| `src/CCE.Infrastructure/Notifications/Messaging/Consumers/FeedConsumer.cs` | Fan-out consumer |
| `src/CCE.Infrastructure/Notifications/Messaging/Consumers/VoteConsumer.cs` | Vote counter consumer |
| `src/CCE.Infrastructure/Notifications/Messaging/Consumers/RankingConsumer.cs` | Hot leaderboard rebuild |
| `src/CCE.Application/Community/EventHandlers/PostVotedBusPublisher.cs` | Vote event bridge |
| `src/CCE.Infrastructure/Caching/RedisOutputCacheInvalidator.cs` | Cache invalidation |
| `src/CCE.Api.Common/Caching/RedisOutputCacheMiddleware.cs` | Output cache middleware |
| `src/CCE.Application/Common/Caching/CacheRegions.cs` | Region definitions |
| `src/CCE.Application/Common/Behaviors/CacheInvalidationBehavior.cs` | Invalidation pipeline |
| `src/CCE.Application/Content/UserContentInterestResolver.cs` | Interest resolver |
| `src/CCE.Application/Content/Public/Queries/ListPublicNews/ListPublicNewsQueryHandler.cs` | News ranking |
