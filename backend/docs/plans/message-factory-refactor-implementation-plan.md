# MessageFactory Refactor & Hardening — Implementation Plan

## Goal

Harden the `MessageFactory` / `SystemCode` / `Resources.yaml` message pipeline so it is
production-grade: no silent failures, a single source of truth, compiler-enforced keys, and
no leftover parallel systems.

The pipeline today works and is well-structured, but has four gaps:

1. **Silent degradation** — typos and missing translations ship to the client instead of failing.
2. **Stringly-typed keys duplicated across 4 places** with no enforcement they agree.
3. **Two parallel systems** still alive (`MessageFactory`/`Response<T>` vs. the legacy
   `Errors`/`Error`/`ErrorType` + prefixed yaml keys).
4. **Manual 4–5-file edits** to add a single message — the same problem `permissions.yaml`
   already solved with a source generator.

## Current State (baseline)

| Concern | File |
|---------|------|
| Envelope builder | `src/CCE.Application/Messages/MessageFactory.cs` |
| Code constants | `src/CCE.Application/Messages/SystemCode.cs` |
| Domain key → code map | `src/CCE.Application/Messages/SystemCodeMap.cs` |
| Domain key constants | `src/CCE.Application/Errors/ApplicationErrors.cs` |
| Response envelope | `src/CCE.Application/Common/Response.cs` |
| HTTP status mapping | `src/CCE.Api.Common/Extensions/ResponseExtensions.cs` |
| Translations | `src/CCE.Api.Common/Localization/Resources.yaml` (390 keys) |
| Localization runtime | `src/CCE.Infrastructure/Localization/LocalizationService.cs` |
| **Legacy (to remove)** | `src/CCE.Application/Common/Errors.cs` (no injection sites) |
| **Legacy keys (to remove)** | 51 prefixed yaml keys (`IDENTITY_*`, `CONTENT_*`, …) |

### The two silent fallbacks (root cause of #1)

- `SystemCodeMap.ToSystemCode` returns `ERR900` for any unmapped key (`SystemCodeMap.cs:204`).
- `LocalizationService.GetString` returns the **raw key string** when the yaml entry is missing
  (`LocalizationService.cs:26`).

Combined: `NotFound<T>("USER_NOT_FUOND")` → `code: "ERR900"`, `message: "USER_NOT_FUOND"`,
no exception, no log, no failing test.

---

## Phase 1 — Integrity Test (highest ROI, do first)

**Why first:** converts the silent-failure class into a build failure with zero production-code
risk. Everything after is safer once this net exists.

**Add** `tests/CCE.Application.Tests/Messages/SystemCodeMapIntegrityTests.cs`:

1. **Every domain key in `SystemCodeMap` resolves in `Resources.yaml`** for both `ar` and `en`
   (load the yaml the same way `YamlLocalizationStore` does; assert non-empty and not equal to
   the key).
2. **No two domain keys map to the same system code** (today this only throws lazily inside the
   static `CodeToDomain` initializer — make it an explicit, eager assertion).
3. **Every `SystemCode.*` constant value equals its field name** (guards copy-paste drift like
   `ERR040 = "ERR041"`).
4. *(Optional)* every yaml key that looks like a code/domain key has a reverse mapping — flag
   orphans.

**Acceptance:** test project builds and passes; deliberately introducing a typo'd key or a
missing translation makes it fail.

---

## Phase 2 — Make Fallbacks Observable

**Why:** even with the test, runtime resilience should be *loud*. A defensive fallback is fine
in production; a *silent* one is not.

1. `SystemCodeMap.ToSystemCode` — when a key is unmapped, log a warning before returning
   `ERR900`. (Inject `ILogger` is awkward in a static class; preferred options below.)
   - **Option A (recommended):** make `MessageFactory` log via its injected dependencies and
     keep `SystemCodeMap` pure — `MessageFactory` already calls both `ToSystemCode` and
     `Localize`, so it is the natural choke point. Add a debug-time guard there that logs when
     `ToSystemCode` returns `ERR900` for a non-`INTERNAL_ERROR` key, or when `Localize` returns
     the key unchanged.
   - **Option B:** in `Development`, throw instead of falling back, so missing keys never reach a
     developer's eyes as a shipped `ERR900`. Gate on `IHostEnvironment`.
2. Add an `ILogger<MessageFactory>` to `MessageFactory` (DI already registers it scoped —
   `DependencyInjection.cs:28`).

**Acceptance:** an unmapped key produces a warning log line (and in Dev, optionally an
exception); production behavior (graceful `ERR900`) unchanged.

---

## Phase 3 — Single Source of Truth via Source Generator (DEFERRED — only if churn is high)

> **Decision: do NOT build this by default.** It is a *maintainability/DX* investment, **not** a
> performance one — see "Generator vs. hand-written: the honest trade-off" below. Build it only
> if the message set churns frequently (new messages weekly, multiple contributors hitting the
> 4-file edit). For a relatively stable set, Phases 1, 2, and 4 deliver the production-readiness
> value and this phase is over-engineering.

**Why (if pursued):** adding one message currently means editing `SystemCode.cs` +
`SystemCodeMap.cs` + `ApplicationErrors.cs` + `Resources.yaml` (+ optional shortcut) with no
compiler safety net. The repo already generates `permissions.yaml` → `Permissions` +
`RolePermissionMap` via `src/CCE.Domain.SourceGenerators/PermissionsGenerator.cs` — mirror that
pattern. The generator's *unique* value is collapsing that to a one-file edit; it does **not**
make the running app faster.

### Generator vs. hand-written: the honest trade-off

A source generator runs at **compile time** and emits the *same kind* of C# you'd write by hand
(`const string` fields + a `Dictionary`). The running application therefore executes essentially
identical code either way — **runtime performance is a tie.** A generator *could* emit a `switch`
expression or `FrozenDictionary` for marginally faster lookup and zero static-init allocation,
but on a code that runs once per HTTP response this is nanoseconds — immaterial.

| Axis | Hand-written files | Source generator |
|------|--------------------|------------------|
| Runtime performance | `Dictionary` O(1), one-time init | Identical (or trivially faster via `switch`/`FrozenDictionary`) — **tie** |
| Build time | Zero | Small per-build cost (parse yaml, emit C#) |
| Cleanliness / DX | 4–5 files synced per message | **One file** — decisive win |
| Correctness guarantees | None at compile time | Can enforce uniqueness/completeness at build |
| Cost to own | Trivial (just C#) | **Non-trivial** — Roslyn generator, netstandard2.0, pinned Roslyn 4.8 |

**Critical point:** the correctness guarantees a generator gives are *also* delivered by **Phase 1
(the integrity test)** — at ~5% of the cost and with no generator to maintain. So Phase 1 removes
the urgency; the generator's only remaining justification is edit-friction at high churn.

**Design:**

1. **New single file** `messages.yaml` at the solution root:
   ```yaml
   messages:
     USER_NOT_FOUND:
       code: ERR001
       type: NotFound
       ar: "المستخدم غير موجود"
       en: "User not found"
     EVALUATION_SUBMITTED:
       code: CON008
       type: Success
       ar: "..."
       en: "..."
   ```
2. **New generator** `MessagesGenerator.cs` (incremental, `netstandard2.0`, pinned Roslyn 4.8 —
   same constraints as `PermissionsGenerator`) that emits:
   - `SystemCode` constants (replaces hand-written `SystemCode.cs`),
   - the `SystemCodeMap` dictionary body (domain key → code),
   - *(optional)* a `MessageType` lookup so `MessageFactory.Fail` no longer needs the caller to
     pass the type for keys that have a canonical type,
   - *(optional)* strongly-typed `MessageKeys` constants to replace bare string literals.
3. **Keep `Resources.yaml` generated from `messages.yaml`** (or have the generator emit a
   `Resources.g.yaml`) so translations and codes can never drift.

**Migration within this phase:**
- Port existing `SystemCode.cs` + `SystemCodeMap.cs` + the `ar`/`en` of `Resources.yaml` into
  `messages.yaml` (one-time mechanical move; Phase 1 test guards correctness).
- Delete the hand-written `SystemCode.cs` / `SystemCodeMap.cs` once the generated equivalents
  compile and the Phase 1 test passes against them.

**Acceptance:** build emits `SystemCode`/`SystemCodeMap` from `messages.yaml`; Phase 1 test
passes unchanged; adding a message is a one-file edit.

> **Decide at the Phase 2/4 boundary.** Default path skips Phase 3 entirely. Revisit only if,
> after living with Phases 1/2/4, the 4-file edit friction is a recurring pain — i.e. churn is
> high enough to amortize owning a Roslyn generator.

---

## Phase 4 — Remove the Parallel Legacy System

**Why:** `Errors`/`Error`/`ErrorType` and the prefixed yaml keys are migration leftovers that
double maintenance and confuse new code.

1. **Delete** `src/CCE.Application/Common/Errors.cs` — confirmed no constructor-injection sites
   (only self-references). Remove its DI registration (`DependencyInjection.cs:27`).
2. **Remove the 51 prefixed yaml keys** (`IDENTITY_USER_NOT_FOUND`, `CONTENT_NEWS_NOT_FOUND`, …)
   from `Resources.yaml` once nothing reads them. The unprefixed keys
   (`USER_NOT_FOUND`, `NEWS_NOT_FOUND`) are the survivors used by `MessageFactory`.
3. **Standardize `MessageFactory` on constants, not literals.** Today usage is mixed:
   `EvaluationSubmitted()` uses `ApplicationErrors.Evaluation.EVALUATION_SUBMITTED`
   (`MessageFactory.cs:120`) while `UserNotFound<T>()`/`EmailUpdated()` use bare literals
   (`:71`, `:116`). Pick the constant form everywhere (or the generated `MessageKeys` from
   Phase 3).
4. **Legacy `Result<T>` track (separate, optional):** `Result<T>` + `Domain/Common/Error.cs` +
   `ErrorType` + `ResultExtensions` + `ResultValidationBehavior` still have live usages and a
   dedicated plan (`result-pattern-unified-errors-implementation-plan.md`). Do **not** fold that
   into this refactor — note it as a follow-up so `Error`-named domain types
   (`ChannelSendResult.Error`, `NotificationLog`) are not mistaken for the legacy envelope.

**Acceptance:** solution builds with `TreatWarningsAsErrors=true`; `Errors.cs` and prefixed keys
gone; no `MessageFactory` bare-literal keys remain; all existing handler tests green.

---

## Execution Order & Dependencies

```
Phase 1 (test)  ──►  Phase 2 (observability)  ──►  Phase 4 (legacy removal)   [default path]

Phase 3 (generator) ── deferred; only if churn justifies it, after Phase 4
```

- **Default path: Phases 1 → 2 → 4.** These deliver the production-readiness value.
- Phase 1 is a prerequisite safety net for everything else.
- Phase 2 is independent and small.
- Phase 4 relies on the Phase 1 test to prove no regressions.
- **Phase 3 is deferred** — runtime performance is a tie with hand-written code, and Phase 1
  already provides its correctness guarantees. Build it only at high message churn, as a pure
  edit-friction / DX improvement layered on top of the completed default path.

## Verification (each phase)

```powershell
dotnet build CCE.sln                     # warnings are errors — must be clean
dotnet test tests/CCE.Application.Tests   # includes new SystemCodeMap integrity test
dotnet test CCE.sln                       # full suite before merge
```

## Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Removing prefixed yaml keys breaks a hidden reader | Phase 1 test + grep for prefixed keys before deleting; the old `Errors` is the only known reader and it is being deleted. |
| Source generator drifts from installed SDK Roslyn | Mirror `PermissionsGenerator` exactly: `netstandard2.0`, Roslyn 4.8, incremental. Do not upgrade. |
| Duplicate codes hidden until runtime | Phase 1 makes the duplicate-code check eager and explicit. |
| Phase 3 mechanical port introduces translation drift | Phase 1 integrity test runs against the generated output; build fails on any missing/empty translation. |
| Confusing `Result<T>` legacy removal with this work | Explicitly out of scope; tracked under the existing result-pattern plan. |

## Definition of Done

- [ ] `SystemCodeMapIntegrityTests` exists and passes; a typo or missing translation fails it.
- [ ] Unmapped keys / missing translations are logged (and throw in Development if Option B chosen).
- [ ] `MessageFactory` uses key constants exclusively — no bare string literals.
- [ ] `src/CCE.Application/Common/Errors.cs` and its DI registration removed.
- [ ] Prefixed `Resources.yaml` keys removed; only unprefixed keys remain.
- [ ] *(If Phase 3 done)* `SystemCode`/`SystemCodeMap` generated from `messages.yaml`; adding a
      message is a single-file edit.
- [ ] `dotnet build CCE.sln` clean (warnings-as-errors); `dotnet test CCE.sln` green.
