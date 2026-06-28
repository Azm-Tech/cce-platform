# Follow / Feed Cycle Test Report

**Date:** 2026-06-22 15:58:33
**Duration:** 184.5s
**External API:** http://localhost:5001
**Internal API:** http://localhost:5002
**Community ID:** 5cc0629c-881d-4ad9-9ea6-96703bba87fe

## Roles

| Role | User ID | Feed path |
|------|---------|-----------|
| Observer (cce-user) | aaaaaaaa-aaaa-aaaa-aaaa-000000000005 | Reads /api/me/feed |
| RegularAuthor (cce-admin) | aaaaaaaa-aaaa-aaaa-aaaa-000000000001 | Non-expert - fan-out via Redis |
| ExpertAuthor (cce-expert) | aaaaaaaa-aaaa-aaaa-aaaa-000000000004 | Expert - fan-in via SQL merge |

---

## Summary

| Metric | Value |
|--------|-------|
| Total API calls | 31 |
| Succeeded | 30 |
| Failed |  |
| Gaps detected | 0 |
| Avg response | 5720ms |
| p50 | 5632ms |
| p95 | 12083ms |
| Max | 12085ms |

---

## Feed Behavior Matrix

| Post | Author | State when created | In feed while following | In feed after unfollow | Mechanism |
|------|--------|--------------------|------------------------|------------------------|-----------|
| Post_A (9a5b4c8d-b1a4-4240-bb6c-a4a339428cfe) | RegularAuthor | Following | YES | NO (immediate) | SQL fallback (live UserFollows) |
| Post_B (cb2299a8-e868-482b-9dbe-d5a193b3ba0b) | ExpertAuthor  | Following | YES | NO (immediate) | SQL expert-merge (live followedUserIds) |
| Post_C (4c2cbb5b-0a78-4a46-a8af-30f0cfc67ca0) | RegularAuthor | Unfollowed | n/a | NO | Fan-out skipped, not in SQL fallback |
| Post_D (fcea8508-6b39-44de-ad29-14c5ea9fc6d7) | ExpertAuthor  | Unfollowed | n/a | NO | Not in expert-merge, not fanned out |

**Note:** Both regular and expert unfollow take effect immediately because the SQL fallback
path dominates when the Redis personal feed sorted-set is cold. The Redis fan-out (feed:user:{id})
is a warm-path optimization - when warm, old entries CAN persist after unfollow (24h TTL).

---

## Response Times by Phase

| Phase | Calls | OK | Avg ms | Max ms |
|-------|-------|----|--------|--------|
| 0 - Health | 2 | 2 | 671 | 797 |
| 1 - Setup | 9 | 8 | 866 | 5451 |
| 2 - Fan-out (regular user follow) | 10 | 10 | 10225 | 12085 |
| 3 - Fan-in (expert follow, SQL read-merge) | 3 | 3 | 5999 | 12083 |
| 4 - Unfollow regular (author leaves feed immediately) | 3 | 3 | 5988 | 12073 |
| 5 - Unfollow expert (SQL merge stops immediately) | 3 | 3 | 5997 | 12069 |
| 6 - Empty feed (both unfollowed) | 1 |  | 11996 | 11996 |

---

## Gaps and Anomalies

> No gaps detected - fan-out, fan-in, and unfollow behavior all matched expected values.

---

## Full Call Log

| # | Phase | Label | Method | Status | ms |
|---|-------|-------|--------|--------|----|
| 1 | 0 - Health | Health External | GET | OK | 797 |
| 2 | 0 - Health | Health Internal | GET | OK | 545 |
| 3 | 1 - Setup | Discover topicId from global feed | GET | OK | 5451 |
| 4 | 1 - Setup | Create test community | POST | OK | 291 |
| 5 | 1 - Setup | Observer joins community | POST | OK | 545 |
| 6 | 1 - Setup | RegularAuthor joins community | POST | FAIL 409 | 138 |
| 7 | 1 - Setup | ExpertAuthor joins community | POST | OK | 533 |
| 8 | 1 - Setup | RegularAuthor follows community | PUT | OK | 364 |
| 9 | 1 - Setup | ExpertAuthor follows community | PUT | OK | 334 |
| 10 | 1 - Setup | Cleanup: unfollow RegularAuthor | PUT | OK | 71 |
| 11 | 1 - Setup | Cleanup: unfollow ExpertAuthor | PUT | OK | 69 |
| 12 | 2 - Fan-out (regular user follow) | Observer follows RegularAuthor | PUT | OK | 282 |
| 13 | 2 - Fan-out (regular user follow) | RegularAuthor creates Post_A | POST | OK | 5893 |
| 14 | 2 - Fan-out (regular user follow) | Poll Observer feed for Post_A | GET | OK | 12085 |
| 15 | 2 - Fan-out (regular user follow) | Poll Observer feed for Post_A | GET | OK | 11994 |
| 16 | 2 - Fan-out (regular user follow) | Poll Observer feed for Post_A | GET | OK | 11995 |
| 17 | 2 - Fan-out (regular user follow) | Poll Observer feed for Post_A | GET | OK | 12000 |
| 18 | 2 - Fan-out (regular user follow) | Poll Observer feed for Post_A | GET | OK | 11988 |
| 19 | 2 - Fan-out (regular user follow) | Poll Observer feed for Post_A | GET | OK | 12013 |
| 20 | 2 - Fan-out (regular user follow) | Poll Observer feed for Post_A | GET | OK | 12001 |
| 21 | 2 - Fan-out (regular user follow) | Poll Observer feed for Post_A | GET | OK | 11995 |
| 22 | 3 - Fan-in (expert follow, SQL read-merge) | Observer follows ExpertAuthor | PUT | OK | 282 |
| 23 | 3 - Fan-in (expert follow, SQL read-merge) | ExpertAuthor creates Post_B | POST | OK | 5632 |
| 24 | 3 - Fan-in (expert follow, SQL read-merge) | Observer feed after Post_B | GET | OK | 12083 |
| 25 | 4 - Unfollow regular (author leaves feed immediately) | Observer unfollows RegularAuthor | PUT | OK | 278 |
| 26 | 4 - Unfollow regular (author leaves feed immediately) | RegularAuthor creates Post_C (after unfollow) | POST | OK | 5612 |
| 27 | 4 - Unfollow regular (author leaves feed immediately) | Observer feed after unfollow Regular | GET | OK | 12073 |
| 28 | 5 - Unfollow expert (SQL merge stops immediately) | Observer unfollows ExpertAuthor | PUT | OK | 279 |
| 29 | 5 - Unfollow expert (SQL merge stops immediately) | ExpertAuthor creates Post_D (after unfollow) | POST | OK | 5644 |
| 30 | 5 - Unfollow expert (SQL merge stops immediately) | Observer feed after unfollow Expert | GET | OK | 12069 |
| 31 | 6 - Empty feed (both unfollowed) | Observer final feed | GET | OK | 11996 |

---

*Generated by test-follow-feed-cycle.ps1*
