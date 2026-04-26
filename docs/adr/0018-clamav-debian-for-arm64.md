# ADR-0018: clamav-debian image for arm64 multi-arch

- **Status:** Accepted
- **Date:** 2026-04-26
- **Sub-project owner:** Foundation
- **Spec ref:** [Foundation §4.1](../superpowers/specs/2026-04-24-foundation-design.md#4-architecture)

## Context

Spec §4.1 originally referenced `clamav/clamav:stable` for the dev antivirus daemon. The official `clamav/clamav:stable` is Alpine-based and publishes **amd64 only** — on arm64 hosts, the pull fails with `no matching manifest for linux/arm64/v8`.

The same upstream ClamAV maintainers publish `clamav/clamav-debian:stable` with **true multi-arch manifests** (amd64 + arm64). It exposes an identical `clamd` daemon on TCP 3310, the same `PING`/`PONG` protocol, the same signature update mechanism (`freshclam`), and the same `/var/lib/clamav` data path. Discovered during Phase 01 planning.

## Decision

- **Local dev:** `image: clamav/clamav-debian:stable` in `docker-compose.yml`.
- **Production:** Either variant works depending on host architecture; both accept the same client config. Choice deferred to ops (sub-project 8 / future deployment cycle).

## Consequences

### Positive

- Pulls cleanly on both amd64 and arm64.
- Identical clamd protocol means downstream .NET client code is unchanged (no branching on arch).
- Identical freshclam signature pipeline — no signature update behavior diverges.

### Negative

- Slightly larger image than the Alpine variant (~200 MB more on disk).
- Maintained alongside the Alpine variant by the same team; future deprecation possible but unannounced as of decision date.

### Neutral / follow-ups

- Trivy scans both variants identically ([ADR-0011](0011-security-scanning-pipeline.md)); no special handling needed.
- If upstream publishes a multi-arch Alpine variant later, revisit.

## Alternatives considered

### Option A: Run amd64 `clamav/clamav:stable` under emulation

- Rejected: emulation is slow for AV scanning; defeats the purpose; arm64 CI runners can't run it.

### Option B: Skip ClamAV in dev, mock the AV client

- Rejected: file-upload tests should exercise the real protocol; mocking would let real bugs through.

### Option C: Build a custom multi-arch ClamAV image

- Rejected: maintenance burden; the official `clamav-debian` variant covers it.

## Related

- [ADR-0005](0005-local-first-docker-compose.md)
- [`docs/superpowers/plans/2026-04-24-foundation/phase-01-docker-compose.md`](../superpowers/plans/2026-04-24-foundation/phase-01-docker-compose.md) (preamble — Divergence 3)
- `docker-compose.yml`
