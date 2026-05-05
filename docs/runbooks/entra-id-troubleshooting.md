# Entra ID troubleshooting runbook (Sub-11)

Common failure modes when running CCE on Entra ID. This runbook replaces `ad-federation.md` (deleted in Phase 04 — Keycloak no longer in use).

## Sign-in fails with "AADSTS50011: redirect URI mismatch"

The Entra ID app registration's redirect URIs don't include the hostname the user is trying to sign in to. Fix:

1. Run `infra/entra/apply-app-registration.ps1 -Environment <env>` to PATCH the manifest. The script substitutes `{{HOSTNAME_*}}` from the env-file at apply time — confirm the env-file has the correct hostnames.
2. If a single redirect URI is missing for a one-off URL, add it manually in the Entra ID portal → App registrations → CCE Knowledge Center → Authentication → Web → Redirect URIs.

## Sign-in fails with "AADSTS70011: invalid scope"

Token request asked for a scope the app registration didn't grant. Most common cause: the app's `requiredResourceAccess[].resourceAccess` list in the manifest doesn't include the requested scope. Fix: edit `infra/entra/app-registration-manifest.json`, re-run `apply-app-registration.ps1`, ask the user to clear cookies and retry.

## Graph user-create returns `Authorization_RequestDenied`

The runtime CCE app is missing `User.ReadWrite.All` admin consent. Fix:

1. Entra ID portal → App registrations → CCE Knowledge Center → API permissions
2. Confirm `User.ReadWrite.All` (Application) is listed
3. Click **Grant admin consent for <tenant>**
4. Wait ~5 minutes for the consent to propagate

If the runtime app instead has the **wrong tenant**'s admin consent: delete the app reg, re-run `apply-app-registration.ps1` against the correct tenant.

## `EntraIdUserResolver` log: "Concurrent EntraIdObjectId link race"

Two requests with the same `oid` claim raced to insert the link row. The filtered unique index rejected the duplicate. Action: none — the resolver swallows the `DbUpdateException` at Information level. The next request for the same user sees the linked state and short-circuits.

If the log fires repeatedly (more than ~10/min) it indicates a stampede. Investigate upstream cookie / session expiry behavior.

## Branding doesn't render after `Configure-Branding.ps1` success

Entra ID caches branding for ~1 hour at the CDN edge. Either:
- Wait 1 hour, or
- Test in a private/incognito window with cleared cookies
- Verify the tenant has Entra ID P1 or P2 SKU (`Configure-Branding.ps1` will exit 0 with a warning if absent; check the script's log file at `C:\ProgramData\CCE\logs\entra-branding-*.log`)

## Conditional Access policy doesn't enforce MFA

Validation: Entra ID portal → Security → Conditional Access → policies → confirm a policy targets the CCE app and the assignment is set to "Require multi-factor authentication".

If the policy exists but isn't firing, check:
- Policy state is **On** (not Report-only)
- The user's tenant is a member tenant of the CA policy (not a guest tenant — Entra ID doesn't extend CA across tenants; ADR-0060)
- The user's account isn't excluded by the assignment filter

## Multi-tenant: partner user gets "AADSTS50105: User not assigned to a role"

The CCE app registration has `appRoleAssignmentRequired: true` (or the partner tenant's admin enabled it). Either:
- Disable role-assignment-required on the app registration, OR
- Have the partner tenant's admin assign at least one app role to the user

## Token-cache misses spike after a deploy

Microsoft.Identity.Web's in-memory token cache empties on app restart. Expected behavior; signs of a healthy cutover. Monitor: cache hit rate should recover within 10 minutes as users re-sign-in.

## See also

- `entra-id-cutover.md` — maintenance-window procedure
- `infra/entra/README.md` — provisioning script reference
- ADR-0058, ADR-0059, ADR-0060
