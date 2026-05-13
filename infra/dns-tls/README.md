# CCE infra/dns-tls/

Operator checklist for cert + DNS provisioning. Sub-10c uses operator-procured certs (not auto-provisioned); pick one of three paths per IDD/site requirements.

## Hostnames (per IDD v1.2)

| Environment | External | Admin Panel | API External | API Admin |
|---|---|---|---|---|
| `prod` | `CCE-ext` | `CCE-admin-Panel` | `api.CCE` | `Api.CCE-admin-Panel` |
| `preprod` | `cce-ext-preprod` | `cce-admin-panel-preprod` | `api.cce-preprod` | `api.cce-admin-panel-preprod` |
| `test` | `cce-ext-test` | `cce-admin-panel-test` | `api.cce-test` | `api.cce-admin-panel-test` |
| `dr` | `cce-ext-dr` | `cce-admin-panel-dr` | `api.cce-dr` | `api.cce-admin-panel-dr` |

(IDD's "port 433" treated as 443 per session memory.)

## Cert procurement — pick one path

### Path A: AD CS auto-enrollment (recommended for AD-joined hosts)

If the host is AD-domain-joined and the AD environment has Active Directory Certificate Services with a Web Server template:

1. Verify auto-enrollment policy applied: `gpresult /h gpreport.html` → review.
2. Trigger immediate enrollment:
   ```powershell
   certutil -pulse
   ```
3. Verify cert appeared in the personal store:
   ```powershell
   Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Subject -match 'CN=' + $env:COMPUTERNAME }
   ```
4. Copy the thumbprint into `.env.<env>` as `IIS_CERT_THUMBPRINT=<thumbprint>`.

### Path B: `win-acme` (Let's Encrypt for internet-facing hosts)

If the host is reachable from the public internet (or has a DNS provider supporting DNS-01 challenge):

1. Download `win-acme` from <https://www.win-acme.com/>; extract to `C:\Tools\win-acme\`.
2. Run interactively to add a cert:
   ```powershell
   cd C:\Tools\win-acme
   .\wacs.exe
   ```
   Pick "Create new certificate" → "Manual input" → enter the 4 hostnames comma-separated → choose IIS as the validation method (HTTP-01) or DNS-01 if non-public.
3. `win-acme` installs the cert + creates a scheduled task for auto-renewal every 60 days.
4. Find the thumbprint:
   ```powershell
   Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Subject -match 'CCE' }
   ```
5. Copy into `.env.<env>` as `IIS_CERT_THUMBPRINT=<thumbprint>`.

### Path C: Manual cert import (purchased cert or internal CA)

For purchased commercial certs or internal CA-issued certs delivered as PFX:

1. Place the PFX file at `C:\ProgramData\CCE\certs\cce-<env>.pfx`.
2. Lock down ACLs:
   ```powershell
   icacls C:\ProgramData\CCE\certs\cce-<env>.pfx /inheritance:r `
       /grant:r "Administrators:F" "<deploy-user>:R"
   ```
3. In `.env.<env>` set:
   ```
   IIS_CERT_PFX_PATH=C:\ProgramData\CCE\certs\cce-<env>.pfx
   IIS_CERT_PFX_PASSWORD=<the-pfx-password>
   ```
4. `Configure-IISSites.ps1` imports the PFX into the cert store on the next run.

## DNS provisioning

Sub-10c does NOT automate DNS. The operator + DNS-admin team provision A records (or AAAA for IPv6) pointing the 4 hostnames at the host IP (or the load balancer's VIP if one fronts the host).

Recommended TTL during steady-state: 300 sec (5 min). Lower TTLs (e.g., 60 sec) before a planned DR failover so propagation is fast.

## Validation

After cert + DNS are in place + `Configure-IISSites.ps1` has run:

```powershell
# 1. From the host: cert is bound to the IIS site.
Get-WebBinding | Format-Table Name, Protocol, BindingInformation, CertificateHash

# 2. From the host: TLS handshake works against each hostname.
foreach ($h in @('CCE-ext','CCE-admin-Panel','api.CCE','Api.CCE-admin-Panel')) {
    Test-NetConnection -ComputerName $h -Port 443
}

# 3. From a CLIENT (outside the host): DNS resolves + TLS terminates.
Resolve-DnsName CCE-ext
Invoke-WebRequest https://CCE-ext/ -UseBasicParsing | Select-Object StatusCode, Headers
```

## Cert renewal

| Cert source | Renewal mechanism |
|---|---|
| AD CS auto-enrollment | Automatic via group policy; no operator action |
| `win-acme` (Let's Encrypt) | Scheduled task auto-renews 60 days before expiry |
| Manual import | Operator must replace the PFX + re-run `Configure-IISSites.ps1` |

For manual renewal, see [`secret-rotation.md`](../../docs/runbooks/secret-rotation.md) — the `IIS_CERT_PFX_PASSWORD` rotation procedure also covers cert renewal mechanics.

## See also

- [`Configure-IISSites.ps1`](../iis/Configure-IISSites.ps1) — provisions the IIS sites with these certs.
- [ADR-0054 — IIS reverse proxy](../../docs/adr/0054-iis-reverse-proxy-on-windows-server.md)
- [Sub-10c design spec §Network](../../project-plan/specs/2026-05-04-sub-10c-design.md#network--iis-reverse-proxy--tls)
