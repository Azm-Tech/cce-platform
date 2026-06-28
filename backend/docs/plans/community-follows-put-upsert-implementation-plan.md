# Community Follows → PUT Upsert Refactor — Implementation Plan

## 1. Goal

Replace the current **POST (follow) + DELETE (unfollow)** endpoint pairs for every
community follow target with a **single idempotent `PUT` upsert** whose request body
carries a `status`. The handler sets the follow state based on that status:

```
PUT /api/me/follows/topics/{topicId}
{ "status": "Followed" }     // creates the follow if absent  (idempotent)
{ "status": "Unfollowed" }   // removes the follow if present (idempotent)
```

`PUT` is the correct verb: the request is idempotent and declares the *desired end
state* of the (user, target) follow relationship rather than an action.

### Decisions (confirmed)
1. **Body shape:** status enum — `{ "status": "Followed" | "Unfollowed" }`.
2. **Scope:** all four targets — **Topic, User, Post, Community**.
3. **Response style:** standardize **all** handlers on `Response<VoidData>` +
   `MessageFactory` + `ToHttpResult` (per memory §A layering). This converts the
   Topic/User/Post handlers off their current `Unit` / `Results.Ok` style.

## 2. Current State (as-is)

| Target | Follow / Unfollow routes | Command return | Handler deps | Counters |
|--------|--------------------------|----------------|--------------|----------|
| Topic | `POST` / `DELETE /api/me/follows/topics/{topicId}` | `Unit` → `Results.Ok` / `NoContent` | `ICommunityWriteService`, clock | none |
| User | `POST` / `DELETE /api/me/follows/users/{userId}` | `Unit` | `ICommunityWriteService`, `ICceDbContext`, clock | follower/following counts on `User` |
| Post | `POST` / `DELETE /api/me/follows/posts/{postId}` | `Unit` | `ICommunityWriteService`, clock | none |
| Community | `POST` / `DELETE /api/community/communities/{id}/follow` | `Response<VoidData>` → `ToHttpResult` | `ICommunityRepository`, `ICceDbContext`, clock, `MessageFactory` | follower count on `Community` |

All four are already **idempotent** in both directions (find-then-skip on follow,
find-then-no-op on unfollow), so the upsert semantics are a natural consolidation.

Relevant files:
- Endpoints: `src/CCE.Api.External/Endpoints/CommunityWriteEndpoints.cs`
- Commands/handlers: `src/CCE.Application/Community/Commands/{Follow,Unfollow}{Topic,User,Post,Community}/`
- Write service: `src/CCE.Application/Community/ICommunityWriteService.cs`,
  `src/CCE.Infrastructure/Community/CommunityWriteService.cs`
- Repo (community): `src/CCE.Application/Community/ICommunityRepository.cs`,
  `src/CCE.Infrastructure/Community/CommunityRepository.cs`
- Domain factories: `TopicFollow`, `UserFollow`, `PostFollow`, `CommunityFollow` in `src/CCE.Domain/Community/`
- Tests: `tests/CCE.Application.Tests/Community/Commands/Write/FollowUnfollowCommandHandlerTests.cs`,
  `tests/CCE.Api.IntegrationTests/Endpoints/CommunityWriteEndpointTests.cs`

## 3. Target State (to-be)

| Target | Route | Command |
|--------|-------|---------|
| Topic | `PUT /api/me/follows/topics/{topicId}` | `SetTopicFollowCommand(Guid TopicId, FollowStatus Status)` |
| User | `PUT /api/me/follows/users/{userId}` | `SetUserFollowCommand(Guid UserId, FollowStatus Status)` |
| Post | `PUT /api/me/follows/posts/{postId}` | `SetPostFollowCommand(Guid PostId, FollowStatus Status)` |
| Community | `PUT /api/community/communities/{id}/follow` | `SetCommunityFollowCommand(Guid CommunityId, FollowStatus Status)` |

All four commands return `Response<VoidData>`; all four endpoints are logic-free
(§A.4) and end with `return result.ToHttpResult();`.

### Shared enum
New file `src/CCE.Application/Community/Commands/FollowStatus.cs`:

```csharp
namespace CCE.Application.Community.Commands;

/// <summary>Desired follow relationship state for a follow upsert (PUT).</summary>
public enum FollowStatus
{
    Followed = 0,
    Unfollowed = 1,
}
```

Bind it from JSON by name (`"Followed"`/`"Unfollowed"`). The APIs already register a
`JsonStringEnumConverter` globally — **verify** in `CCE.Api.Common` JSON setup; if not
present, add `[JsonConverter(typeof(JsonStringEnumConverter))]` on the request record
property or register the converter. (Verification step, see §6.)

### Request DTO
One shared request record in the endpoints file (or a small shared DTO):

```csharp
public sealed record SetFollowRequest(FollowStatus Status);
```

## 4. Step-by-Step Changes

### Step 0 — Add the `FollowStatus` enum
Create `src/CCE.Application/Community/Commands/FollowStatus.cs` as above.

### Step 1 — Topic: merge into `SetTopicFollow`
1. New folder `Commands/SetTopicFollow/`:
   - `SetTopicFollowCommand(Guid TopicId, FollowStatus Status) : IRequest<Response<VoidData>>`
   - `SetTopicFollowCommandHandler` — merge the bodies of the existing
     `FollowTopicCommandHandler` + `UnfollowTopicCommandHandler`:
     ```
     userId = currentUser.GetUserId() ?? NotAuthenticated
     if Status == Followed:
         existing = FindTopicFollowAsync(...)
         if existing is null: SaveFollowAsync(TopicFollow.Follow(...))
     else: // Unfollowed
         RemoveTopicFollowAsync(...)   // already no-ops when absent
     return _msg.Ok(ApplicationErrors.General.SUCCESS_OPERATION)
     ```
   - Inject `MessageFactory` (new), keep `ICommunityWriteService`, `ICurrentUserAccessor`, `ISystemClock`.
2. Delete `Commands/FollowTopic/` and `Commands/UnfollowTopic/`.

### Step 2 — Post: merge into `SetPostFollow`
Same shape as Topic, using `FindPostFollowAsync` / `RemovePostFollowAsync` /
`PostFollow.Follow`. Use `ApplicationErrors.Community.POST_NOT_FOUND` only if you add a
post-existence check (current handlers don't — keep parity unless we decide to validate;
see §7 open question). Delete `Commands/FollowPost/` + `Commands/UnfollowPost/`.

### Step 3 — User: merge into `SetUserFollow` (keep denormalized counters)
1. New `Commands/SetUserFollow/`:
   - `SetUserFollowCommand(Guid UserId, FollowStatus Status) : IRequest<Response<VoidData>>`
   - Handler merges follow + unfollow, preserving the count maintenance currently in
     both handlers:
     - **Followed:** if not already following → `SaveFollowAsync(UserFollow.Follow(...))`,
       then `follower.IncrementFollowing()` + `followed.IncrementFollowers()`,
       `SaveChangesAsync`. The `UserFollow.Follow` self-follow guard
       (`FollowerId != FollowedId`) still throws `DomainException` — preserve that test.
     - **Unfollowed:** `RemoveUserFollowAsync(...)`; if it returned `true`,
       `follower.DecrementFollowing()` + `followed.DecrementFollowers()`, `SaveChangesAsync`.
   - Deps: `ICommunityWriteService`, `ICceDbContext`, `ICurrentUserAccessor`, `ISystemClock`, `MessageFactory`.
2. Delete `Commands/FollowUser/` + `Commands/UnfollowUser/`.
   > Note: self-follow currently surfaces as an unhandled `DomainException`. If we want a
   > clean 4xx instead, map it to `_msg.BadRequest`/a new error key — see §7.

### Step 4 — Community: merge into `SetCommunityFollow` (keep counter + existence check)
1. New `Commands/SetCommunityFollow/`:
   - `SetCommunityFollowCommand(Guid CommunityId, FollowStatus Status) : IRequest<Response<VoidData>>`
   - Handler merges the two existing `Response<VoidData>` handlers:
     - load+validate community (`NotFound COMMUNITY_NOT_FOUND` when null/inactive) —
       keep on the **Followed** path as today; on **Unfollowed** keep the existing
       behavior (find follow, remove, `DecrementFollowers`).
     - `IncrementFollowers` / `DecrementFollowers` exactly as the current handlers.
   - Deps unchanged: `ICommunityRepository`, `ICceDbContext`, `ICurrentUserAccessor`, `ISystemClock`, `MessageFactory`.
2. Delete `Commands/FollowCommunity/` + `Commands/UnfollowCommunity/`.

### Step 5 — Rewrite endpoints (`CommunityWriteEndpoints.cs`)
Replace the four POST/DELETE pairs with four PUTs. All logic-free, all return
`ToHttpResult()`. Drop the manual `currentUser.GetUserId()` 401 guards in the
`/me/follows` endpoints — authentication is enforced by `.RequireAuthorization()` and
the handler returns `NotAuthenticated` defensively (matches the Community pattern).

```csharp
// /api/me/follows group
follows.MapPut("/topics/{topicId:guid}", async (
    Guid topicId, SetFollowRequest body, IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new SetTopicFollowCommand(topicId, body.Status), ct).ConfigureAwait(false);
    return result.ToHttpResult();
}).WithName("SetTopicFollow");

// ...users, posts analogous...

// /api/community group
community.MapPut("/communities/{id:guid}/follow", async (
    Guid id, SetFollowRequest body, IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new SetCommunityFollowCommand(id, body.Status), ct).ConfigureAwait(false);
    return result.ToHttpResult();
}).RequireAuthorization(Permissions.Community_Community_Join).WithName("SetCommunityFollow");
```

Update the `using` block: remove the eight `Follow*`/`Unfollow*` namespaces, add the
four `Set*` ones. Add `public sealed record SetFollowRequest(FollowStatus Status);` near
the bottom of the file (alongside `MarkAnswerRequest` / `EditReplyRequest`).

### Step 6 — `ICommunityWriteService` / `CommunityWriteService`
**No signature changes required.** The merged handlers reuse the existing
`FindXFollowAsync` / `SaveFollowAsync<T>` / `RemoveXFollowAsync` methods. Leave the
service as-is.

## 5. Tests to Update

### Unit — `FollowUnfollowCommandHandlerTests.cs`
Rename to `SetFollowCommandHandlerTests.cs` (or keep filename, update contents).
Rewrite each pair of tests against the new `Set*` handlers, parameterizing on
`FollowStatus`:
- `SetTopicFollow_Followed_saves_new_follow`
- `SetTopicFollow_Followed_idempotent_when_already_following`
- `SetTopicFollow_Unfollowed_calls_remove`
- `SetTopicFollow_Unfollowed_idempotent_when_not_following`
- analogous for User (incl. **self-follow throws/returns error**, count inc/dec on both
  directions), Post, and add Community handler tests (existence check + counters).
- All handlers now return `Response<VoidData>` → assert `result.IsSuccess` /
  `result.Data` instead of relying on `Unit`.
  > Per memory: `CCE.Application.Tests` is pre-existingly broken — validate via the
  > domain tests + a clean prod build, and run this file's tests in isolation if the
  > project compiles.

### Integration — `CommunityWriteEndpointTests.cs`
The existing anonymous-401 tests (lines 77–126) use `PostAsync`/`DeleteAsync` against
`/api/me/follows/...`. Update them to `PutAsync` with a JSON body
`{ "status": "Followed" }`, asserting 401 still returned for anonymous. Add an
authenticated happy-path PUT test per target if the harness supports it.

### Other references to check (grep before finishing)
- `GetMyFollowsQueryHandlerTests` / `GetMyFollows` — read-side, **unchanged** (still
  lists current follows); confirm no coupling to the deleted commands.
- Any FE/client contract docs under `docs/` referencing the old POST/DELETE follow
  routes — note the breaking change.

## 6. Verification

1. `dotnet build CCE.sln` — must pass (warnings = errors). Confirms no dangling
   references to deleted command namespaces.
2. Confirm `JsonStringEnumConverter` is globally registered so `"Followed"` binds; if
   not, add the converter (see §3).
3. `dotnet test tests/CCE.Domain.Tests` — domain follow tests still green.
4. Run the External API, exercise via Swagger:
   - `PUT /api/me/follows/topics/{id}` with `{"status":"Followed"}` then `{"status":"Unfollowed"}` twice each (idempotency).
   - `PUT /api/community/communities/{id}/follow` both statuses; verify follower count increments/decrements once only.
   - `GET` the my-follows endpoint to confirm state reflects the upserts.

## 7. Open Questions / Risks

1. **Breaking API change.** POST/DELETE follow routes are removed. Any existing
   FE/mobile client must migrate to PUT + body. Confirm no external consumer depends on
   the old verbs, or version the route if needed.
2. **Self-follow on User.** Currently throws an unhandled `DomainException` (→ 500).
   Recommend mapping it to a `Response` error (`_msg.BadRequest`/new `CANNOT_FOLLOW_SELF`
   key) while we're touching the handler. Decide: keep throwing vs. graceful 4xx.
3. **Post/Topic existence validation.** The current follow handlers don't verify the
   target exists (only Community does). Keep that parity, or add `NOT_FOUND` checks for
   symmetry? Lean toward parity to minimize scope unless you want the validation.
4. **Permissions unchanged.** `/me/follows/*` endpoints carry only `RequireAuthorization()`
   (no specific permission); Community follow keeps `Community_Community_Join`. No
   permission.yaml changes.

## 8. File Change Summary

**Add (5):**
- `src/CCE.Application/Community/Commands/FollowStatus.cs`
- `src/CCE.Application/Community/Commands/SetTopicFollow/{SetTopicFollowCommand,SetTopicFollowCommandHandler}.cs`
- `src/CCE.Application/Community/Commands/SetPostFollow/...`
- `src/CCE.Application/Community/Commands/SetUserFollow/...`
- `src/CCE.Application/Community/Commands/SetCommunityFollow/...`

**Delete (8 command folders):**
- `Commands/{FollowTopic,UnfollowTopic,FollowPost,UnfollowPost,FollowUser,UnfollowUser,FollowCommunity,UnfollowCommunity}/`

**Edit:**
- `src/CCE.Api.External/Endpoints/CommunityWriteEndpoints.cs` (routes + usings + request record)
- `tests/CCE.Application.Tests/Community/Commands/Write/FollowUnfollowCommandHandlerTests.cs`
- `tests/CCE.Api.IntegrationTests/Endpoints/CommunityWriteEndpointTests.cs`

**Unchanged:**
- `ICommunityWriteService` / `CommunityWriteService`, `ICommunityRepository` / impl,
  all domain follow entities, all read-side (`GetMyFollows`) code.
