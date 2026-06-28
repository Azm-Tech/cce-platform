# Review — MessageFactory / Response&lt;T&gt; Pattern

> Format: each item is a **Bug** (what's wrong + where) followed by a **Fix** (what to do).
> Severity legend: 🔴 confirmed bug · 🟠 inconsistency · 🟡 hardening.

---

## 1. 🔴 `AD_LOGIN_SUCCESS` is unmapped and untranslated

**Bug**
`AdLoginCommandHandler.cs:32` returns `_msg.Ok(result.Token!, "AD_LOGIN_SUCCESS")` on the **success** path, but the key `AD_LOGIN_SUCCESS` exists in **neither**:
- `src/CCE.Application/Messages/SystemCodeMap.cs` (only `LOGIN_SUCCESS` → `CON056` is present), nor
- `src/CCE.Api.Common/Localization/Resources.yaml` (only `LOGIN_SUCCESS:` is present).

Result on a successful AD login:
- `SystemCodeMap.ToSystemCode` falls back to **`ERR900`** (internal-error code) — an error code on a successful login.
- `Localize` returns the raw string `"AD_LOGIN_SUCCESS"` as the user-facing message and logs a warning.

**Fix**
Either reuse the existing key:
```csharp
LoginFailureReason.None => _msg.Ok(result.Token!, "LOGIN_SUCCESS"),
```
or register `AD_LOGIN_SUCCESS` properly:
- add `["AD_LOGIN_SUCCESS"] = SystemCode.CONxxx,` to `SystemCodeMap.cs`, and
- add an `AD_LOGIN_SUCCESS:` ar/en entry to `Resources.yaml`.

---

## 2. 🟠 Ad-hoc string keys instead of constants

**Bug**
Success/error keys are passed as raw string literals rather than `ApplicationErrors` constants, e.g. `"CONTENT_CREATED"`, `"CONTENT_DELETED"`, `"ITEMS_LISTED"`, `"AD_LOGIN_SUCCESS"`. They mostly resolve, but item #1 proves how a single typo silently degrades to `ERR900` with no compile-time protection.

**Fix**
Promote the recurring keys to constants in `ApplicationErrors` (or a `MessageKeys` static class) and reference those everywhere. A misspelled constant then fails the build instead of failing silently at runtime.

---

## 3. 🟡 Silent degradation hides missing keys

**Bug**
`MessageFactory.ResolveCode` falls back to `ERR900` and `Localize` echoes the key when a key is missing — only a `LogWarning` is emitted. Warnings are easily lost, so missing-key defects (like #1) reach production unnoticed.

**Fix**
Add a startup self-check or unit test asserting **bidirectional** consistency:
- every domain key referenced in code/`Resources.yaml` has a `SystemCodeMap` entry, and
- every `SystemCodeMap` key has a `Resources.yaml` translation (ar + en).

This converts the whole class of bug into a build/test failure. Recommended location: `tests/CCE.Application.Tests` (or a dedicated guard test) so CI catches it.

---

## 4. 🟡 Mixed success-message conventions

**Bug**
Three styles coexist for the same purpose: convenience shortcuts (`_msg.UserNotFound<T>()`), ad-hoc keys (`_msg.Ok(data, "CONTENT_CREATED")`), and `ApplicationErrors` constants (`_msg.Ok(ApplicationErrors.General.SUCCESS_OPERATION)`). No single rule, which makes the surface harder to maintain.

**Fix**
Document one convention (suggest: convenience shortcuts for domain-specific outcomes, constants for generic ones — never raw literals) and align handlers opportunistically as they're touched.

---

## Not a bug (verified, leaving as-is)

- **All `Response<T>` handlers consistently use `MessageFactory`** — no manual `Response<T>` construction was found outside `Response.cs` / `MessageFactory.cs`. Good.
- **`ResponseValidationBehavior`** correctly maps FluentValidation failures into localized `FieldError[]`. Good.
