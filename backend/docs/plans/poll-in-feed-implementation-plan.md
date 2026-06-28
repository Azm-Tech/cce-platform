# Poll Data in Feed Listings — Implementation Plan

## Context

Posts have three types (`PostType`: `Info=0`, `Question=1`, `Poll=2`). A `Poll` post owns exactly one `Poll` aggregate with 2–10 `PollOption` rows and accumulates `PollVote` rows per user per option.

Today the feed path (`FeedHydratorService` → `CommunityFeedItemDto`) and the topic listing path (`PublicPostDto`) carry no poll fields. Clients must call the separate `GET /api/community/polls/{id}/results` endpoint after rendering the card to get option data — an extra round-trip per visible poll post.

**Goal:** embed a `PollSummaryDto` on every `PostType.Poll` item that passes through the hydrator, covering both the community/user feed and the topic listing endpoint. Non-poll posts carry `Poll = null`. All existing hydration logic stays unchanged.

---

## What we already have (read-only, no changes)

| Piece | Location | Notes |
|---|---|---|
| `Poll` domain entity | `CCE.Domain/Community/Poll.cs` | `PostId`, `Deadline`, `AllowMultiple`, `IsAnonymous`, `ShowResultsBeforeClose`, `Options` nav |
| `PollOption` entity | `CCE.Domain/Community/PollOption.cs` | `Label`, `SortOrder`, `VoteCount` (denormalized) |
| `PollVote` entity | `CCE.Domain/Community/PollVote.cs` | `PollId`, `PollOptionId`, `UserId` |
| `PollConfiguration` | `CCE.Infrastructure/…/PollConfiguration.cs` | `ux_poll_post` unique index on `PostId`; cascade delete options |
| `GetPollResultsQueryHandler` | `…/GetPollResults/` | Returns `PollResultsDto` for the detail endpoint — not reused here |
| EF DbSet `Polls` | `ICceDbContext` | Already present (used by `GetPollResultsQueryHandler`) |
| `PostType.Poll = 2` | `CCE.Domain/Community/PostType.cs` | Fixed at creation, never changed |

---

## Step 0 — Verify `ICceDbContext` exposes `PollVotes`

**File:** `src/CCE.Application/Common/Interfaces/ICceDbContext.cs`

The `GetPollResultsQueryHandler` reaches vote counts through `p.Options.Sum(o => o.VoteCount)` (denormalized), never through `PollVotes`. The hydrator **does** need `PollVotes` to tell the current user which options they already selected.

Check whether `IQueryable<PollVote> PollVotes { get; }` exists on `ICceDbContext`. If not, add it (and the matching `DbSet<PollVote>` on `CceDbContext`).

> **Why the denormalized VoteCount is enough for counts but not for user state:** `VoteCount` is an `int` on `PollOption` (source of truth = PollVote rows kept in sync by the domain). We read it directly in the projection without hitting `PollVotes`. But "did this user vote for option X?" requires joining `PollVotes` by `(PollId, UserId)`.

---

## Step 1 — New DTOs

**File:** `src/CCE.Application/Community/Public/Dtos/PollSummaryDto.cs` *(new file)*

```csharp
namespace CCE.Application.Community.Public.Dtos;

/// <summary>Lightweight poll snapshot embedded in feed / topic-listing items.</summary>
public sealed record FeedPollOptionDto(
    System.Guid Id,
    string      Label,
    int         SortOrder,
    int         VoteCount,    // 0 when ResultsVisible = false
    double      Percentage,   // 0 when ResultsVisible = false
    bool        UserVoted);   // true when the authenticated user selected this option

public sealed record PollSummaryDto(
    System.Guid                                        PollId,
    System.DateTimeOffset                              Deadline,
    bool                                               IsClosed,
    bool                                               AllowMultiple,
    bool                                               IsAnonymous,
    bool                                               ShowResultsBeforeClose,
    bool                                               ResultsVisible,  // IsClosed || ShowResultsBeforeClose
    int                                                TotalVotes,      // 0 when !ResultsVisible
    System.Collections.Generic.IReadOnlyList<FeedPollOptionDto> Options);
```

**Design notes:**
- `UserVoted` lives on each option (not a list of IDs) — avoids a nested set lookup on the client.
- `ResultsVisible` is pre-computed so the client doesn't need to re-evaluate the deadline.
- `TotalVotes` is hidden when `!ResultsVisible`, keeping the closed/open states visually clean.
- Kept separate from `PollResultsDto` (the detail endpoint DTO) because they serve different contracts and the feed version adds `UserVoted` + `IsAnonymous`.

---

## Step 2 — Extend the two feed/listing DTOs

### `CommunityFeedItemDto`

**File:** `src/CCE.Application/Community/Public/Dtos/CommunityFeedItemDto.cs`

Add one nullable trailing parameter:

```csharp
public sealed record CommunityFeedItemDto(
    // … all existing parameters unchanged …
    int  VoteStatus,
    PollSummaryDto? Poll);   // ← new; null for Info and Question posts
```

### `PublicPostDto`

**File:** `src/CCE.Application/Community/Public/Dtos/PublicPostDto.cs`

```csharp
public sealed record PublicPostDto(
    // … all existing parameters unchanged …
    System.Collections.Generic.IReadOnlyList<System.Guid> AttachmentIds,
    System.DateTimeOffset CreatedOn,
    PollSummaryDto? Poll);   // ← new
```

> Both DTOs are `record` types — adding a trailing parameter is a pure positional change. Every call site that constructs these records must be updated (compiler will catch them all as errors).

---

## Step 3 — Update `FeedHydratorService`

**File:** `src/CCE.Application/Community/Public/FeedHydratorService.cs`

### 3a — Add `ISystemClock` dependency

```csharp
private readonly ISystemClock _clock;

public FeedHydratorService(ICceDbContext db, IRedisFeedStore feedStore, ISystemClock clock)
{
    _db = db;
    _feedStore = feedStore;
    _clock = clock;
}
```

`ISystemClock` is needed to compute `IsClosed = clock.UtcNow >= poll.Deadline` in a testable way.

### 3b — Step 6: Poll data (conditional — only when poll posts are present)

Insert **after** step 5 (votes) and **before** the final map, using `_clock.UtcNow` captured once:

```csharp
// ── Step 6: Poll data (skipped entirely when no Poll-type posts on this page) ──
var now = _clock.UtcNow;

var pollPostIds = enriched
    .Where(e => e.Type == PostType.Poll)
    .Select(e => e.Id)
    .ToList();

// pollsByPostId: keyed by PostId for O(1) lookup in the final map.
var pollsByPostId = new System.Collections.Generic.Dictionary<System.Guid, PollRow>();

if (pollPostIds.Count > 0)
{
    var rawPolls = await _db.Polls
        .Where(p => pollPostIds.Contains(p.PostId))
        .Select(p => new
        {
            p.Id,
            p.PostId,
            p.Deadline,
            p.AllowMultiple,
            p.IsAnonymous,
            p.ShowResultsBeforeClose,
            Options = p.Options
                .OrderBy(o => o.SortOrder)
                .Select(o => new { o.Id, o.Label, o.SortOrder, o.VoteCount })
                .ToList(),
            TotalVotes = p.Options.Sum(o => o.VoteCount),
        })
        .ToListAsyncEither(ct)
        .ConfigureAwait(false);

    // User votes (skipped when anonymous or no polls).
    var userVotedOptionIds = new System.Collections.Generic.Dictionary<System.Guid, System.Collections.Generic.HashSet<System.Guid>>();
    if (userId.HasValue && rawPolls.Count > 0)
    {
        var pollIds = rawPolls.Select(p => p.Id).ToList();
        var votes = await _db.PollVotes
            .Where(v => pollIds.Contains(v.PollId) && v.UserId == userId.Value)
            .Select(v => new { v.PollId, v.PollOptionId })
            .ToListAsyncEither(ct)
            .ConfigureAwait(false);

        foreach (var v in votes)
        {
            if (!userVotedOptionIds.TryGetValue(v.PollId, out var set))
                userVotedOptionIds[v.PollId] = set = new System.Collections.Generic.HashSet<System.Guid>();
            set.Add(v.PollOptionId);
        }
    }

    foreach (var raw in rawPolls)
    {
        var isClosed       = now >= raw.Deadline;
        var resultsVisible = isClosed || raw.ShowResultsBeforeClose;
        var totalVotes     = resultsVisible ? raw.TotalVotes : 0;

        userVotedOptionIds.TryGetValue(raw.Id, out var votedSet);
        votedSet ??= new System.Collections.Generic.HashSet<System.Guid>();

        var options = raw.Options.Select(o => new FeedPollOptionDto(
            o.Id,
            o.Label,
            o.SortOrder,
            resultsVisible ? o.VoteCount : 0,
            resultsVisible && raw.TotalVotes > 0
                ? System.Math.Round(o.VoteCount * 100.0 / raw.TotalVotes, 1)
                : 0,
            votedSet.Contains(o.Id)))
            .ToList();

        pollsByPostId[raw.PostId] = new PollSummaryDto(
            raw.Id, raw.Deadline, isClosed,
            raw.AllowMultiple, raw.IsAnonymous, raw.ShowResultsBeforeClose,
            resultsVisible, totalVotes, options);
    }
}
```

### 3c — Pass poll into the DTO map

In the final `Select` that builds `CommunityFeedItemDto`, append:

```csharp
pollsByPostId.GetValueOrDefault(e.Id));
// null for Info/Question posts; PollSummaryDto for Poll posts
```

### Round-trip budget after this change

| Step | What | Conditional? |
|---|---|---|
| 1 | Posts + communities + users + topics + expertProfiles JOIN | Always |
| 2 | Redis meta batch (concurrent with 3-5) | Always |
| 3 | Attachments | Always |
| 4 | Tags | Always |
| 5a | Post follows (watchlist) | Authenticated only |
| 5b | Post votes | Authenticated only |
| **6a** | **Polls + Options** | **Only if ≥ 1 Poll post on page** |
| **6b** | **PollVotes (user selections)** | **Authenticated + ≥ 1 Poll post** |

Pages with zero poll posts pay no extra cost. Step 6a and 6b cannot overlap with the Redis batch (same EF DbContext, not thread-safe) but are conditional enough that this is acceptable.

---

## Step 4 — Update `PublicPostDto` construction sites

The topic listing path constructs `PublicPostDto` outside `FeedHydratorService`. Find every query handler that builds `PublicPostDto` (grep: `new PublicPostDto(`) and apply the same poll-fetch pattern:

1. After loading post rows, collect `pollPostIds` where `Type == PostType.Poll`.
2. If non-empty, fetch polls + options in one query.
3. Fetch user's voted option IDs if authenticated.
4. Pass `pollsByPostId.GetValueOrDefault(postId)` as the last constructor argument.

**Handlers to update (confirm via compiler errors after Step 2):**
- `GetPublicPostByIdQueryHandler` — post detail; poll data is most critical here.
- Any topic/post listing handler that returns `PublicPostDto[]` / `PagedResult<PublicPostDto>`.

---

## Step 5 — Build verification

```powershell
dotnet build src/CCE.Application/CCE.Application.csproj
```

The record changes in Step 2 will surface every construction site as a compile error. Fix them all (no suppression). Expected zero warnings since the project treats warnings as errors.

---

## Step 6 — Smoke test

After starting the APIs, confirm:

```powershell
# Feed with a mix of post types
curl http://localhost:5001/api/me/feed?sort=1&page=1&pageSize=20 -H "Authorization: Bearer dev:cce-user"
# → Poll posts should have .poll = { pollId, deadline, isClosed, options: [...], totalVotes, ... }
# → Info/Question posts should have .poll = null

# Community feed
curl "http://localhost:5001/api/community/feed?communityId=<id>&sort=1" -H "Authorization: Bearer dev:cce-user"

# Topic listing (PublicPostDto path)
curl "http://localhost:5001/api/community/topics/<id>/posts?page=1&pageSize=10" -H "Authorization: Bearer dev:cce-user"
```

Key assertions:
- `Type = 2` posts: `poll` is an object with `pollId`, `deadline`, `options`, `totalVotes`, `isClosed`.
- `Type = 0/1` posts: `poll` is `null`.
- Closed polls (`deadline < now` OR `showResultsBeforeClose = true`): `voteCount` and `percentage` are real numbers.
- Open polls with `showResultsBeforeClose = false`: `voteCount = 0`, `percentage = 0`, `totalVotes = 0`.
- Authenticated user who voted: their option(s) have `userVoted = true`.

---

## What does NOT change

- `GetPollResultsQueryHandler` and its `PollResultsDto` — unchanged, still the canonical detail endpoint.
- Redis fan-out / FeedConsumer — poll data is not cached in Redis; it is always read from SQL on hydration. Poll vote counts change too frequently and are already denormalized on `PollOption.VoteCount`, so reading them fresh per page is cheap and always consistent.
- `IRedisFeedStore` — no new keys needed.
- All existing `CommunityFeedItemDto` consumers — the field is appended last; only construction sites change.
- Domain entities, migrations, EF configuration — no schema change. Polls table already exists.

---

## File change summary

| File | Change |
|---|---|
| `ICceDbContext.cs` | Verify/add `IQueryable<PollVote> PollVotes` |
| `CceDbContext.cs` | Verify/add `DbSet<PollVote> PollVotes` |
| `PollSummaryDto.cs` | **New file** — `FeedPollOptionDto` + `PollSummaryDto` |
| `CommunityFeedItemDto.cs` | Add `PollSummaryDto? Poll` trailing parameter |
| `PublicPostDto.cs` | Add `PollSummaryDto? Poll` trailing parameter |
| `FeedHydratorService.cs` | Add `ISystemClock`, Steps 6a+6b, pass `Poll` to DTO map |
| `GetPublicPostByIdQueryHandler.cs` | Add poll fetch + pass to `PublicPostDto` |
| Any topic-listing handler | Same poll fetch pattern as above |
