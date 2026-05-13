# ADR-0025: Deterministic SHA-256 Guids for seed data

- **Status:** Accepted
- **Date:** 2026-04-28
- **Sub-project owner:** Data & Domain
- **Spec ref:** [Data & Domain §5.7](../../project-plan/specs/2026-04-27-data-domain-design.md)

## Context

Seeders must be idempotent — re-running them must NOT produce duplicate rows. Two patterns: query by natural key (e.g., `WHERE iso_alpha3 = 'SAU'`) or query by deterministic Id derived from the natural key.

## Decision

`DeterministicGuid.From(string seed)` computes `SHA-256(seed)[0..16]` and returns a Guid. Each seeder generates entity Ids from a structured key (e.g., `"country:SAU"`, `"template:ACCOUNT_CREATED"`, `"km_node:cce-basics:reduce"`). Re-running queries by Id, finds the existing row, and skips.

## Consequences

### Positive
- Idempotency check is a single primary-key lookup (cheaper than text-key WHERE).
- Same seed key produces same Guid across environments — handy for cross-environment ID stability.
- SHA-256 not used for security here — purely for hash-to-Guid distribution.

### Negative
- Changing a seed key produces a different Guid → re-running creates a duplicate. Seed keys are append-only by convention.
- CA5350 (weak SHA-1) was originally hit; we use SHA-256 to satisfy the analyzer (despite SHA-1 being equivalent for this non-security use).
