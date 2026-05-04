# Forward-only migrations — discipline + escape hatch

> Sub-10b's rollback story relies on this. Read before authoring a new EF migration.

## The contract

Every CCE migration must be **forward-only and backward-compatible**: an old image (release N-1) running against the new schema (release N) keeps working. This is what makes image-tag rollback (Sub-10b §6) safe — no DB rewind required.

## Rules

1. **Additive only.** Add columns, add tables, add indexes. Never drop or rename in place.
2. **No destructive defaults.** A new `NOT NULL` column needs a `DEFAULT` expression that handles existing rows. Prefer `WITH VALUES` (SQL Server) so the default is materialized into existing rows at migration time, not at row-read time.
3. **Online indexes.** `CREATE INDEX WITH (ONLINE = ON)` against tables with non-trivial row counts — locks are blocked otherwise and we don't have a maintenance window.
4. **Deprecation across releases.** To remove or rename a column:
   - Release **N**: add the replacement column. Application code dual-writes to old + new.
   - Release **N+1**: stop reading the old column. Application code reads from new only.
   - Release **N+2**: drop the old column. Schema is now clean.
5. **No type changes in place.** Add a new column of the new type, dual-write, swap reads, drop old (3-release sequence).
6. **No FK direction flips.** Same 3-release sequence as type changes.

## Why these rules

With these rules, an image from release N-1 can run against schema from release N because:
- New columns it doesn't know about are ignored.
- Old columns it still writes to still exist and have correct types.
- Indexes are additive, not subtractive.

This means `deploy/rollback.ps1 -ToTag <previous-tag>` works without DB intervention.

## Escape hatch — destructive migrations

Some changes can't be staged. Examples:
- Splitting one table into two.
- Migrating data between tenant buckets.
- Backfilling computed columns from row data.

When this is necessary, the change is its own release with these gates:

1. **Separate spec + plan.** `docs/superpowers/specs/<date>-<topic>-design.md` documents the data-migration plan; PR-reviewed before any code.
2. **Backup-and-restore is part of the runbook** — schema rollback isn't possible, so the rollback strategy is "restore the pre-deploy backup". Backup automation lives in Sub-10c, but the destructive release explicitly invokes it.
3. **Maintenance window.** Operator schedules downtime; deploy runs against a frozen system.
4. **No image-tag rollback.** The release explicitly disables `rollback.ps1` for the target tag (or the runbook calls out that it's unavailable).

## When in doubt — ask

A migration with any "wait, will the old image still run?" doubt is a destructive migration. Default to the escape hatch.

## References

- Sub-10b spec §Migration discipline: [`../superpowers/specs/2026-05-03-sub-10b-design.md`](../superpowers/specs/2026-05-03-sub-10b-design.md)
- Rollback runbook (Phase 02): [`./rollback.md`](./rollback.md) (lands in Sub-10b Phase 02)
- EF Core migration docs: <https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/>
