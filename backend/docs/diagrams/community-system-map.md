# Community System Map

## 1. Write Paths

### Post Published

```
POST /api/community/posts/{id}/publish
    │
    ▼
PublishPostCommandHandler
    │  domain aggregate: Post.Publish()
    │  raises: PostCreatedEvent
    │
    ▼
DomainEventDispatcher  (inside SavingChangesAsync, pre-commit)
    │
    ▼
PostCreatedBusPublisher  (INotificationHandler<PostCreatedEvent>)
    │  publishes PostCreatedIntegrationEvent
    │  → captured by MassTransit EF outbox (outbox_message row)
    │
    ▼
SaveChanges commits atomically:
    ├─ posts row  (Status = Published)
    └─ outbox_message row  (pending relay)
    │
    ▼
BusOutboxDeliveryService  (background, polls outbox_message)
    │  stamps sent_time, relays to bus
    │
    ▼
Bus (InMemory dev / RabbitMQ prod)
    │
    ├──► FeedConsumer
    ├──► SignalRConsumer
    └──► NotificationConsumer
```

### Vote Cast / Retracted

```
POST /api/community/posts/{id}/vote
    │
    ▼
VotePostCommandHandler
    │  domain aggregate: Post.RegisterVote(userId, newValue, clock)
    │  raises: PostVotedEvent(PostId, CommunityId, UserId,
    │                          Direction, PreviousDirection,
    │                          UpvoteCount, DownvoteCount, Score)
    │
    ▼
DomainEventDispatcher → PostVotedBusPublisher
    │  publishes VoteCreatedIntegrationEvent → EF outbox
    │
    ▼
SaveChanges commits atomically:
    ├─ post_votes row  (upserted)
    ├─ posts row  (UpvoteCount, DownvoteCount, Score updated)
    └─ outbox_message row
    │
    ▼
Bus → VoteConsumer
```

---

## 2. Consumers

| Consumer | Listens to | Does |
|---|---|---|
| **FeedConsumer** | `PostCreatedIntegrationEvent` | Writes `feed:community:{id}`, fans out to `feed:user:{id}` per follower, writes `hot:{communityId}` with score=0, evicts output cache |
| **VoteConsumer** | `VoteCreatedIntegrationEvent` | Increments/decrements `post:{id}:meta` hash (upvotes / downvotes), updates `hot:{communityId}` with real score |
| **SignalRConsumer** | `PostCreatedIntegrationEvent` | Pushes `NewPost` event to SignalR groups `community:{id}` and `topic:{id}` |
| **NotificationConsumer** | `PostCreatedIntegrationEvent` | Sends notifications to followers |
| ~~RankingConsumer~~ | ~~`PostCreatedIntegrationEvent`~~ | Removed — was dual-writing `hot:{communityId}` alongside VoteConsumer, causing a race. Replaced by the admin rebuild endpoint. |

---

## 3. Redis Keys

| Key | Type | Score / Value | TTL | Written by | Read by |
|---|---|---|---|---|---|
| `feed:community:{communityId}` | Sorted set | `UnixTimestamp(publishedOn)` | 24 h | FeedConsumer | `ListCommunityFeed` Newest fast-path |
| `feed:user:{userId}` | Sorted set | `UnixTimestamp(publishedOn)` | 24 h | FeedConsumer (fan-out) | Personal feed queries |
| `hot:{communityId}` | Sorted set | `Post.Score` (Wilson + decay) | 15 min | FeedConsumer (score=0 on publish), VoteConsumer (real score on vote), Admin rebuild endpoint | `ListCommunityFeed` Hot fast-path |
| `post:{postId}:meta` | Hash | fields: `upvotes`, `downvotes`, `score`, `replyCount` | 1 h | VoteConsumer via `IncrementPostVotesAsync` | `GetPostMetaAsync` (hot-counter cache) |
| `notif:{userId}:count` | String | integer counter | 1 h | `IncrementNotificationCountAsync` | Notification badge queries |

---

## 4. Read Path (feed query)

```
GET /api/community/feed?communityId={id}&sort=Hot|Newest

ListCommunityFeedQueryHandler
    │
    ├─ Redis fast-path — all conditions must be true:
    │     communityId is provided
    │     sort = Hot OR Newest
    │     no tag filter
    │     no postType filter
    │
    ├─ [Hot]    GetHotPostsAsync      → reads hot:{communityId}      TTL 15 min
    ├─ [Newest] GetCommunityFeedAsync → reads feed:community:{id}    TTL 24 h
    │
    │   pagination total = Redis cardinality (SortedSetLengthAsync)
    │   avoids phantom pages from stale IDs that HydrateAsync will drop
    │
    ├─ Redis miss / cache cold → falls through to SQL
    │     ORDER BY Score (Hot) | PublishedOn (Newest) | UpvoteCount (TopVoted)
    │
    └─ HydrateAsync  (always SQL, runs for both paths)
          guard: Published + community IsActive + Visibility=Public
          stale Redis IDs silently drop here
          enriches: author name, attachment IDs, tag IDs, topic names,
                    expert flag, watchlist flag, current user's vote
```

---

## 5. Celebrity / Hybrid Fan-out

```
FeedConsumer decides at consume time:

Is author an Expert (ExpertProfile row exists)?
OR author.FollowerCount > 10,000?
    │
    YES → celebrity path
    │     feed:community:{id}  ✓  written
    │     hot:{id}             ✓  written
    │     feed:user:{*}        ✗  skipped  (O(N) writes for huge follower lists)
    │
    │     personal feeds merged at read time by ListCommunityFeedQueryHandler
    │
    NO  → normal path
          all three written: community feed + hot leaderboard + every follower's personal feed

Both paths evict the output cache (Posts + Feed regions) after fan-out.
```

---

## 6. Output Cache (HTTP layer)

```
Anonymous GET /api/community/feed
    → cached by CCE.Api.Common output-cache middleware
      regions: "posts", "feed"

Invalidated by:
    FeedConsumer          after fan-out completes (including celebrity early-return)
    CacheInvalidationBehavior   any write command that touches Posts / Feed regions
```

---

## 7. Admin Recovery Endpoint

Replaces the removed `RankingConsumer`. Offline repair only — never triggered by an event.

```
POST /api/admin/community/{communityId}/hot-leaderboard/rebuild
POST /api/admin/community/hot-leaderboard/rebuild-all

    ▼
RebuildHotLeaderboardCommandHandler
    reads: top 1000 Published posts ORDER BY Score DESC  (SQL — source of truth)
    writes: hot:{communityId} via AddToHotLeaderboardAsync  (overwrites stale scores)

Permission: Cache_Manage  (cce-admin)
```

**When to run:**
- Redis eviction wiped `hot:{communityId}` before TTL expiry
- Scores drifted (e.g. after a data migration or bug fix that touched `Post.Score`)
- After any bulk vote import or score recalculation
- As a nightly cron if operational risk requires it

---

## 8. Single-Writer Guarantee for `hot:{communityId}`

| Writer | When | Score |
|---|---|---|
| **FeedConsumer** | Post published | `0` (initial placement) |
| **VoteConsumer** | Every vote cast or retracted | `Post.Score` from domain event |
| **Admin rebuild** | Manual / scheduled | `Post.Score` from SQL |

No other code path writes to `hot:{communityId}`. This is the invariant that prevents ranking drift.
