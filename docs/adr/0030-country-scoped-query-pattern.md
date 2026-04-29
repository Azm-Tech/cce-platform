# ADR-0030 — Country-Scoped Query Pattern

**Status:** Accepted
**Date:** 2026-04-29
**Deciders:** CCE backend team

---

## Context

The BRD requires that a `StateRepresentative` user sees only data that belongs to their assigned country. For example, the country dashboard, KAPSARC snapshots, and resource-request lists must automatically filter to the representative's country. Administrators (SuperAdmin, ContentAdmin, etc.) see all countries without restriction.

There are multiple query handlers that read country-scoped data, and the filtering logic must be consistent across all of them. A per-handler ad-hoc approach would be error-prone and easy to omit.

---

## Decision

Introduce `ICountryScopeAccessor` in `CCE.Application.Common.CountryScope`:

```csharp
public interface ICountryScopeAccessor
{
    /// <summary>
    /// Returns the list of country IDs visible to the current principal.
    /// Null  = no scope restriction (admin / system context — all countries visible).
    /// Empty = "see nothing" (StateRep assigned to no country, or deactivated).
    /// Non-empty = filter WHERE country_id IN (...).
    /// </summary>
    IReadOnlyList<Guid>? GetAllowedCountryIds();
}
```

The `HttpContextCountryScopeAccessor` implementation (External API host) reads the `StateRepresentativeAssignments` table for the current user on first call and caches the result in the HTTP request scope. For non-StateRep roles it returns `null` (unrestricted).

Country-scoped query handlers **opt in** by calling `ICountryScopeAccessor.GetAllowedCountryIds()` and applying the filter:

```csharp
var scope = _scopeAccessor.GetAllowedCountryIds();
if (scope is not null)
    query = query.Where(x => scope.Contains(x.CountryId));
```

---

## Consequences

- Country-scoped reads are opt-in — a handler that forgets the call is not restricted, but all handlers that *should* scope are documented with a test asserting the filter is applied.
- `StateRepresentative` users automatically receive correctly-filtered data without any per-endpoint logic.
- Empty list correctly produces zero results rather than all results (fail-closed).
- Admins pass `null` and receive the full dataset, which is the correct default.
- The pattern is extensible: if multi-country assignment is added in the future, only `HttpContextCountryScopeAccessor` needs updating; all handlers remain unchanged.
