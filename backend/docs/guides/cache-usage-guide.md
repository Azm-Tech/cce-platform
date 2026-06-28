# Using the Output Cache (Redis regions + reload/delete)

Practical guide to the Redis-backed HTTP output cache: how it's organised into **regions** ("tables"),
how to clear it from your own code, and the admin endpoints to reload/delete by key.

---

## 0. Mental model

Public GET responses on whitelisted routes are cached in Redis by `RedisOutputCacheMiddleware` under keys
like `out:/api/resources?page=1|lang=en`. Every key is also indexed into a per-entity **region** set
(`out:tag:<region>`) so a whole region can be cleared without scanning Redis.

| Region | Routes it covers |
|---|---|
| `resources` | `/api/resources*` |
| `feed` | `/api/feed/*` (news-events, featured-posts) |
| `posts` | `/api/community/*` (public reads) |
| `news` / `events` / `topics` / `categories` / `countries` / `pages` / `homepage` | the matching `/api/*` prefixes |

Region names live in `CacheRegions` (`CCE.Application/Common/Caching/CacheRegions.cs`) — the single source
of truth shared by the middleware, the invalidator, and your commands.

Authenticated requests (Authorization header or session cookie) bypass the cache entirely, so per-user
data is never cached.

---

## 1. Invalidate from your own code — three ways

### A. Declarative — annotate the command (preferred)
Mark a write command with `ICacheInvalidatingRequest` and list its regions. The
`CacheInvalidationBehavior` clears them automatically **after the handler commits, on success only**.
Your handler needs no cache code.

```csharp
using CCE.Application.Common.Caching;

public sealed record PublishResourceCommand(Guid Id)
    : IRequest<Response<Guid>>, ICacheInvalidatingRequest
{
    public IReadOnlyCollection<string> CacheRegionsToEvict { get; } =
        [CacheRegions.Resources, CacheRegions.Feed];
}
```

Already wired this way: `PublishResourceCommand` (resources+feed), `CreatePostCommand` (posts+feed),
`CreateReplyCommand` (posts). Add the interface to other write commands the same way.

### B. Imperative — inject `IOutputCacheInvalidator`
For conditional or single-key eviction. Call it **after** your `SaveChangesAsync` so the cache reflects
committed state.

```csharp
public sealed class ApproveResourceCommandHandler(IOutputCacheInvalidator cache /*, repo, uow, messages*/)
    : IRequestHandler<ApproveResourceCommand, Response<VoidData>>
{
    public async Task<Response<VoidData>> Handle(ApproveResourceCommand cmd, CancellationToken ct)
    {
        // … mutate aggregate, await uow.SaveChangesAsync(ct) …
        await cache.EvictRegionsAsync([CacheRegions.Resources, CacheRegions.Feed], ct);
        return messages.Ok("SUCCESS_OPERATION");
    }
}
```
`IOutputCacheInvalidator` also exposes `EvictKeyAsync(key, ct)`, `GetStatusAsync(ct)`, `FlushAllAsync(ct)`.

### C. Via MediatR / admin (operational)
The cache CQRS handlers are usable from code too:
```csharp
await mediator.Send(new EvictCacheRegionCommand(CacheRegions.Posts), ct);
await mediator.Send(new FlushCacheCommand(), ct);
```

### Quick rule
- Command mutates a cached entity → **A** (annotate).
- Conditional / single key → **B** (inject).
- Manual / operational → **C** (admin endpoints below).
- **Never** inject `IConnectionMultiplexer`/raw Redis in handlers — go through `IOutputCacheInvalidator`.

---

## 2. Admin endpoints (`/api/admin/cache`, permission `Cache.Manage`)

| Method & route | Action |
|---|---|
| `GET /api/admin/cache/regions` | list regions ("tables") + entry counts |
| `POST /api/admin/cache/regions/{region}/reload` | purge a region → repopulates on next read |
| `DELETE /api/admin/cache/regions/{region}` | purge a region (delete semantics) |
| `DELETE /api/admin/cache/keys?key=<urlencoded>` | delete one specific key |
| `POST /api/admin/cache/flush` | clear every region |

`{region}` must be one of the `CacheRegions` names; an unknown region is rejected by validation.

---

## 3. Verify with redis-cli

```bash
# after GET /api/resources twice (2nd = hit):
redis-cli KEYS 'out:*'                      # entry + out:tag:resources
redis-cli SMEMBERS out:tag:resources        # indexed entry keys

# after POST /api/admin/cache/regions/resources/reload:
redis-cli SMEMBERS out:tag:resources        # (empty) — repopulates on next GET
```

Stop Redis and everything still works: reads bypass the cache, admin/invalidation calls log a warning
and no-op (never a 500). Entries also expire on their own after `Infrastructure:OutputCacheTtlSeconds`.

---

## 4. Add a new cached entity

1. Add a region constant + a `("/api/<prefix>", Region)` entry to `CacheRegions`.
2. Add the route prefix to `OutputCacheOptions.WhitelistPrefixes` (and the `OutputCache` appsettings if overridden).
3. Annotate that entity's write commands with `ICacheInvalidatingRequest` → the new region.
