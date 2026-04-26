# ADR-0017: Serilog file sink as dev SIEM stub (drop Papercut)

- **Status:** Accepted
- **Date:** 2026-04-26
- **Sub-project owner:** Foundation
- **Spec ref:** [Foundation §4.1](../superpowers/specs/2026-04-24-foundation-design.md#4-architecture)

## Context

Spec §4.1 listed `papercut:25` in the dev Compose stack labeled "SIEM stub". On review during Phase 01 planning, this is a category mismatch: **Papercut is an SMTP capture tool**, not a SIEM. SIEMs receive structured security events (failed auth, permission denials, rate-limit trips, etc.), not emails. MailDev already covers SMTP capture in dev; including Papercut would duplicate that role and add no SIEM-shaped surface.

A genuine dev SIEM stand-in is a structured log sink that backend code can write security events to, with shape close enough to what real SIEM (Splunk / ELK / Sentinel) shipping will look like.

## Decision

- **Drop Papercut** entirely from `docker-compose.yml`.
- **Use Serilog's File sink** with a JSON formatter writing to `logs/siem-events.log`. Wired into the .NET Application/Infrastructure layers in phases 06/07.
- Each line is a JSON object with `timestamp`, `eventType`, `userId`, `permission`, `outcome`, plus the relevant context. Real SIEM shipping (HTTP appender, syslog, etc.) is **sub-project 8 (Integration Gateway)**.

## Consequences

### Positive

- The dev stub matches the role: a place to write structured security events.
- Engineers can `tail -f logs/siem-events.log | jq` and see exactly what would ship to a real SIEM.
- No misleading service in `docker-compose.yml`.
- One less container in dev.

### Negative

- The file sink is not durable across `docker compose down -v` if logs live inside a container; we keep `logs/` host-mounted so the file survives.
- Engineers must remember that the file is a stub — sub-project 8 swaps to network shipping.

### Neutral / follow-ups

- Sub-project 8 owns real SIEM ship (Splunk HEC or equivalent).
- Event schema (`siem-event` shape) is finalized in sub-project 2 / 8; Foundation uses a minimal first draft.

## Alternatives considered

### Option A: Keep Papercut, ignore the label mismatch

- Rejected: the spec was wrong; document the correction here.

### Option B: Run a local Splunk/ELK in Compose

- Rejected: heavy (1–2 GB RAM minimum); not Foundation's job; better to stub.

### Option C: Console-only logging

- Rejected: doesn't give a tangible "events file" to point engineers at; harder to demo SIEM-shape thinking.

## Related

- [ADR-0005](0005-local-first-docker-compose.md)
- [`docs/superpowers/plans/2026-04-24-foundation/phase-01-docker-compose.md`](../superpowers/plans/2026-04-24-foundation/phase-01-docker-compose.md) (preamble — Divergence 2)
- `docker-compose.yml`, `logs/`
