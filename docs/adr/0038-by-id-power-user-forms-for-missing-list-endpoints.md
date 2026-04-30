# ADR-0038 — By-ID power-user forms for missing list endpoints

**Status:** Accepted
**Date:** 2026-04-30
**Deciders:** CCE frontend team

---

## Context

Sub-projects 1-4 shipped the Internal API but did not fully expose every workflow needed by the admin CMS. Two explicit gaps:

1. **Country resource requests** (`/api/admin/country-resource-requests/{id}/{approve,reject}`) — only approve/reject endpoints exposed; no list endpoint. State representatives submit requests via the public API; admins were expected to learn about pending requests via notifications, not via a queue.
2. **Community moderation** (`/api/admin/community/{posts,replies}/{id}`) — only soft-delete endpoints. No flag-queue or pending-content list. Admins were expected to receive flag notifications and act on the flagged ID.

Both gaps block a "queue UI" pattern. Two ways to handle:

| Option | Notes |
|---|---|
| Skip the feature in v0.1.0 | Hides the workflow entirely; SuperAdmins cannot act |
| Build a fake-list (poll a different endpoint, e.g., audit log, to reconstruct pending IDs) | Brittle; couples to audit format |
| Build a power-user "by-ID" form | Honest about the gap; unblocks the workflow |

---

## Decision

When the Internal API exposes `act-on-id` endpoints without a corresponding `list` endpoint, the admin-cms ships a **power-user by-ID form** for v0.1.0:

1. The page accepts a GUID input, validated against the standard pattern.
2. Action buttons (approve / reject / soft-delete / etc.) are wired to the existing endpoints.
3. The page displays a `byIdNote` translated string that names the limitation explicitly: *"The Internal API does not yet expose a list endpoint; enter the GUID of the request to process. A list view lands in v0.2.0."*
4. The page is gated by the same permission as the underlying action.

Two pages ship under this pattern in v0.1.0:
- `features/content/country-resource-request.page` (Approve/Reject by GUID, with required admin notes on Reject).
- `features/community-moderation/community-moderation.page` (Soft-delete post or reply by GUID, with confirmation dialog).

---

## Consequences

**Positive:**
- The workflow is reachable and audit-traceable. SuperAdmins can act on flagged content even before a queue UI exists.
- The admin-cms documents what the backend does not yet expose. Future engineers see the marker and know what to build next.
- The pattern is consistent with the state-rep create dialog (Phase 1.5) which also takes free-text GUIDs because the country/expert dropdowns are not yet ready.

**Negative:**
- Admins must paste GUIDs from an external source (audit log, notification email, log monitor). This is OK for SuperAdmins (technical users) but unsuitable for less-technical roles like ContentManager.
- **Mitigation:** Each by-ID form documents that v0.2.0 will replace it with a queue UI once the corresponding list endpoint lands.

**Future work (tracked):**
- Sub-3 follow-up: expose `GET /api/admin/country-resource-requests` (Pending status filter).
- Sub-3 follow-up: expose `GET /api/admin/community/flagged-content` (post + reply unified queue).
- Once exposed, replace each by-ID form with a paged queue + per-row action.
