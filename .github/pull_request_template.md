## Summary

<!-- 1–3 bullet points: what changed and why -->

## Test plan

- [ ] `dotnet test backend/CCE.sln` green
- [ ] `pnpm nx run-many -t lint,test` green
- [ ] If API surface changed: `./scripts/check-contracts-clean.sh` green
- [ ] If UI changed: `pnpm nx run-many -t e2e` (web-portal-e2e + admin-cms-e2e) green
- [ ] Manual smoke notes (if any):

## Security checklist

- [ ] No new secrets / credentials in code
- [ ] AuthN / AuthZ impact considered
- [ ] Input validation on new endpoints
- [ ] Audit-log entry for new state-changing operations

## BRD traceability

<!-- List BRD section IDs covered, or "n/a" -->

## Screenshots / output (optional)
