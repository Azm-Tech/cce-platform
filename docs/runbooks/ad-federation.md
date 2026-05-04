# AD federation troubleshooting (Sub-10c)

This runbook covers issues with Keycloak's LDAP user-federation provider and AD security-group → role mapping. Provisioning is via `infra/keycloak/apply-realm.ps1`. ADR-0055 documents the design decisions.

## Smoke check: federation is alive

```powershell
# 1. Re-apply realm config (idempotent).
.\infra\keycloak\apply-realm.ps1 -Environment <env>

# 2. From the Keycloak admin UI: Realms → cce → User Federation → cce-ldap.
#    "Test connection" + "Test authentication" should both succeed.

# 3. Try a real login: open the assistant portal at https://api.CCE/...
#    enter an AD username + password. Expected: success; user appears
#    in Keycloak admin UI under Users (cached on first login).
```

## Common failures

### `Failed to acquire master-admin token`

**Symptom:** `apply-realm.ps1` exits at the first REST call with a 401 / 403.

**Cause:** `KEYCLOAK_ADMIN_USER` / `KEYCLOAK_ADMIN_PASSWORD` in `.env.<env>` don't match what Keycloak expects.

**Fix:**
1. Open Keycloak admin UI directly; verify the master-admin login works.
2. Update `.env.<env>` with the correct values.
3. `validate-env.ps1` then re-run `apply-realm.ps1`.

### `LDAPS connection failed: certificate verification failed`

**Symptom:** Federation provider creation succeeds but "Test connection" fails with a TLS error.

**Cause:** The AD CA's certificate isn't in Keycloak's truststore.

**Fix:**
1. Export the AD CA root cert as DER (from a domain-joined Windows host: `certmgr.msc` → Trusted Root → export the AD CA).
2. Import into Keycloak's truststore. For Keycloak 26.x running as a host service:
   ```powershell
   keytool -import -alias ad-ca -keystore $JAVA_HOME\lib\security\cacerts -file <path-to-AD-CA.cer>
   ```
3. Restart Keycloak.
4. Re-test "Test connection".

If LDAPS isn't possible immediately, fall back to LDAP on port 389 by setting `LDAP_PORT=389` in `.env.<env>` — but only as a temporary measure; AD bind credentials travel in the clear over LDAP.

### `Bind DN authentication failed`

**Symptom:** "Test authentication" in Keycloak admin UI fails.

**Cause:** `LDAP_BIND_DN` or `LDAP_BIND_PASSWORD` is wrong.

**Fix:**
1. Verify the bind account exists and the password is current. Test from a domain-joined host:
   ```powershell
   # Quick LDAP bind test using PowerShell:
   $cred = Get-Credential   # enter the bind DN + password
   New-Object System.DirectoryServices.DirectoryEntry("LDAP://ad.cce.local", $cred.UserName, $cred.GetNetworkCredential().Password) |
       Select-Object -ExpandProperty Name
   ```
   Expected: prints the directory root. Error → wrong creds.
2. Update `.env.<env>` with corrected values.
3. Re-apply.

### `User authenticates but has no roles`

**Symptom:** A user logs in via Keycloak but the backend rejects requests with 403.

**Cause:** Group mapper isn't finding the user's AD groups.

**Diagnosis:**
1. Keycloak admin UI → cce realm → Users → find the user → "Role Mappings". Should list at least one of `cce-admin`, `cce-editor`, etc.
2. If empty: check the user's AD security-group membership (`Get-ADUser <user> -Properties MemberOf`). Should include one of the `CCE-*` groups documented in ADR-0055.
3. If groups exist in AD but not in Keycloak: `LDAP_GROUPS_DN` may be wrong, or the group-name → role mapping is broken.

**Fix:**
1. Verify `LDAP_GROUPS_DN` matches the OU AD admins use (often `OU=Groups,DC=cce,DC=local`).
2. Re-apply realm config; re-trigger sync from Keycloak admin UI.
3. Have the user re-log-in (cached state may need refresh).

### `User exists in AD but doesn't appear in Keycloak`

**Symptom:** Trying to log in produces "user not found".

**Cause:** Federation provider's `usersDn` is too narrow, or sync hasn't run.

**Fix:**
1. Verify `LDAP_USERS_DN` matches the OU containing the user (e.g., `OU=Users,DC=cce,DC=local`).
2. From Keycloak admin UI → User Federation → cce-ldap → "Synchronize all users". Check the sync log for errors.
3. Try logging in directly — first-login triggers an import for that single user even if a full sync hasn't run.

### `apply-realm.ps1 returns 400 BadRequest`

**Symptom:** Provisioning fails with `Validate Password Policy is applicable only with WRITABLE edit mode` or similar Keycloak validation errors.

**Cause:** The realm JSON has a config attribute that conflicts with `editMode=READ_ONLY`.

**Fix:** Check the realm JSON for incompatible attributes. Known constraint: `validatePasswordPolicy` MUST be `false` when `editMode=READ_ONLY`. The committed `infra/keycloak/realm-cce-ldap-federation.json` has this set correctly; deviations from the committed config will trigger this error.

### Re-apply changes nothing

**Symptom:** `apply-realm.ps1` runs cleanly but a config change in the realm JSON doesn't take effect.

**Cause:** Existing component's config is updated in place via PUT, but Keycloak caches federation provider state.

**Fix:**
1. Restart Keycloak after config changes (or wait for the cache TTL).
2. Verify the change: Keycloak admin UI → User Federation → cce-ldap → check the relevant attribute.

## Escalation

If federation fails entirely and a fix isn't obvious within 30 minutes:

1. **Roll back** the deploy that introduced the change (`rollback.ps1 -Environment <env> -ToTag <prev>`).
2. **File an incident.** Include: env name, last-known-good tag, error logs from `C:\ProgramData\CCE\logs\keycloak-apply-<env>-<UTC>.log`, Keycloak server logs.
3. **Escalate to AD admin** if it's an AD-side issue (cert, bind account, group structure).

## See also

- [ADR-0055 — AD federation via Keycloak LDAP](../adr/0055-ad-federation-via-keycloak-ldap.md)
- [`secret-rotation.md`](secret-rotation.md) — `LDAP_BIND_PASSWORD` rotation procedure.
- [Keycloak User Federation docs](https://www.keycloak.org/docs/latest/server_admin/#_user-storage-federation)
- [Sub-10c design spec §Identity](../superpowers/specs/2026-05-04-sub-10c-design.md#identity--keycloak-ldap-user-federation)
