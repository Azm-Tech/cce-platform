# ADR-0054 — IIS reverse proxy on Windows Server

**Status:** Accepted
**Date:** 2026-05-04
**Deciders:** Sub-10c brainstorm (kilany113@gmail.com)
**Sub-project:** [Sub-10c — Production infra + DR](../../project-plan/specs/2026-05-04-sub-10c-design.md)

## Context

Sub-10b ships 4 backend containers bound to localhost ports (4200/4201/5001/5002). Sub-10c needs them reachable at IDD's production hostnames (`CCE-ext`, `CCE-admin-Panel`, `api.CCE`, `Api.CCE-admin-Panel`) over HTTPS. The host is Windows Server 2022 per IDD; AD-domain-joined.

## Decision

Use IIS as a reverse proxy on the host. Each IDD hostname maps to an IIS site that terminates TLS (port 443) and reverse-proxies via Application Request Routing (ARR) + URL Rewrite to the corresponding localhost backend port.

**Considered alternatives:**

- **Reverse-proxy container (Caddy / Traefik)**: rejected. Requires a new container in the stack with auto-cert lifecycle (ACME). On a Windows Server target with AD CS already present, IIS's native cert-store integration is simpler. Caddy/Traefik shine on Linux; on Windows they fight against the platform.
- **nginx as a host-level Windows service**: rejected. nginx-on-Windows is second-class (no graceful reload, bespoke service-management); cert-renewal scripting is bespoke vs IIS's native cert-binding APIs.

**Why IIS won:**

- Native to Windows Server 2022; the host already has IIS or can install it via standard `Install-WindowsFeature`.
- AD CS auto-enrollment delivers certs into the LocalMachine cert store; IIS binds them by thumbprint with zero copy operations.
- ARR is Microsoft's documented reverse-proxy module; battle-tested in enterprise environments; preserves `X-Forwarded-*` headers via `serverVariables` rules.
- Ops/network admins on a Windows shop already know IIS; no new tooling to learn.
- Single config surface — no Docker reverse-proxy layer in addition to the app containers.

## Implementation

`infra/iis/Install-ARRPrereqs.ps1` (one-time host setup) installs IIS + URL Rewrite 2.1 + ARR 3.0; enables the global ARR proxy.

`infra/iis/Configure-IISSites.ps1` (per-deploy or one-time) reads `IIS_CERT_*` + `IIS_HOSTNAMES` from `.env.<env>`; provisions the 4 sites with HTTPS bindings + SNI + named cert + ARR rewrite rules from `web.config.template`.

`infra/iis/web.config.template` is the parameterized rewrite config — `{BACKEND_PORT}` substituted per site. Adds standard security headers (HSTS, X-Content-Type-Options, X-Frame-Options, Referrer-Policy). SSE-friendly: `appConcurrentRequestLimit=5000`, no response buffering.

## Consequences

**Positive:**
- Reuses existing IIS infrastructure on the AD-joined Windows host.
- Cert procurement uses one of three documented paths (AD CS, win-acme, manual import) — operator chooses per IDD.
- ARR's `X-Forwarded-*` headers preserve the real client IP for backend logs.
- Per-site rollback in `Configure-IISSites.ps1` prevents partial-config breakage.
- Per-host TLS termination simplifies the Sub-10b containers — they stay HTTP-only on localhost.

**Negative / accepted:**
- Adds a configuration surface (IIS + ARR) on top of Docker. Two layers to debug when things break.
- ARR has known SSE quirks; mitigated by `appConcurrentRequestLimit=5000` and by avoiding `precondition` rules that buffer.
- Operators not familiar with IIS need to learn `Get-Website`, `New-WebBinding`, `Set-WebConfigurationProperty`. The runbook + scripts cover the common operations.
- IIS sites + cert bindings are mutable host state outside of compose; `Configure-IISSites.ps1` is the only supported way to manage them.

**Out of scope (Sub-10c+):**
- Cert auto-provisioning (operator-driven; three paths documented).
- DNS auto-provisioning.
- WAF / IP allowlisting (can layer ARR's IP/domain restrictions if IDD requires).
- Multi-host LB (Sub-10c targets one host per env; an LB in front would terminate TLS at the LB).

## References

- [Sub-10c design spec §Network](../../project-plan/specs/2026-05-04-sub-10c-design.md#network--iis-reverse-proxy--tls)
- [`infra/dns-tls/README.md`](../../infra/dns-tls/README.md) — cert + DNS operator checklist.
- [Microsoft URL Rewrite docs](https://learn.microsoft.com/en-us/iis/extensions/url-rewrite-module/using-url-rewrite-module)
- [Microsoft ARR docs](https://learn.microsoft.com/en-us/iis/extensions/installing-application-request-routing-arr/)
- ADR-0055 — AD federation via Keycloak LDAP (Sub-10c)
