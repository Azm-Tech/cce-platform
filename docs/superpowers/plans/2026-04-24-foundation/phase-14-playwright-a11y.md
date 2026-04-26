# Phase 14 — Playwright E2E + axe-core Accessibility

> Parent: [../2026-04-24-foundation.md](../2026-04-24-foundation.md) · Spec: [../../specs/2026-04-24-foundation-design.md](../../specs/2026-04-24-foundation-design.md)

**Phase goal:** Stand up real browser-driven E2E tests against both Angular apps with `@axe-core/playwright` enforcing WCAG 2.1 AA on every page visited. Foundation seeds five smoke specs (root render, locale switch ar↔en, health page, admin redirect to Keycloak, profile page protected). Spec §8.1 + §10's a11y gate are now active.

**Tasks in this phase:** 5
**Working directory:** `/Users/m/CCE/`
**Preconditions:** Phase 13 complete; Docker stack healthy; both Angular apps build clean.

---

## Pre-execution sanity checks

1. `git status` clean.
2. `cd frontend && pnpm nx run-many -t build --projects=web-portal,admin-cms 2>&1 | tail -3` → both build.
3. Docker: `docker compose ps --format json | grep -c '"Health":"healthy"'` → 5.
4. Phase 09 generated `web-portal-e2e` and `admin-cms-e2e` projects: `ls frontend/apps/web-portal-e2e frontend/apps/admin-cms-e2e 2>/dev/null` → both directories present.

---

## Task 14.1: Install `@axe-core/playwright` + add a11y helper

**Files:**
- Modify: `frontend/package.json` (devDependency)
- Create: `frontend/libs/test-utils-e2e/` library OR a shared spec helper file (depending on Nx layout)
- Simpler: per-app `support/axe.ts` helper

**Rationale:** Each Playwright spec runs an `injectAxe` + `checkA11y` call against the rendered page; failures with `critical` or `serious` impact fail the spec. Phase 18 documents the rule list in `docs/a11y-checklist.md`.

- [ ] **Step 1: Install the package**

```bash
cd frontend
pnpm add -D @axe-core/playwright@4.10.1
cd ..
```

- [ ] **Step 2: Create the shared helper at app-e2e level (one per app — they're separate projects)**

Create `frontend/apps/web-portal-e2e/src/support/axe.ts`:

```typescript
import type { Page, TestInfo } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

/**
 * Run axe-core against the current page. Fails the test on any `critical` or `serious`
 * accessibility violation per spec §8.1 (a11y as a CI gate). Lower-severity issues
 * are logged via attachments for triage.
 */
export async function expectNoA11yViolations(page: Page, testInfo: TestInfo, scope?: string): Promise<void> {
  const builder = new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa']);
  if (scope) {
    builder.include(scope);
  }
  const results = await builder.analyze();

  await testInfo.attach('axe-results.json', {
    body: JSON.stringify(results, null, 2),
    contentType: 'application/json',
  });

  const blocking = results.violations.filter((v) => v.impact === 'critical' || v.impact === 'serious');
  if (blocking.length > 0) {
    const summary = blocking.map((v) => `${v.id} (${v.impact}): ${v.description}`).join('\n');
    throw new Error(`a11y violations:\n${summary}`);
  }
}
```

Copy the same file to `frontend/apps/admin-cms-e2e/src/support/axe.ts` (separate project, separate `node_modules` resolution paths under pnpm but the file content is identical).

- [ ] **Step 3: Commit**

```bash
git add frontend/package.json frontend/pnpm-lock.yaml frontend/apps/web-portal-e2e/src/support/ frontend/apps/admin-cms-e2e/src/support/
git -c commit.gpgsign=false commit -m "feat(phase-14): install @axe-core/playwright + per-app a11y helper (WCAG 2.1 AA gate)"
```

---

## Task 14.2: web-portal smoke E2E — root render + locale switch

**Files:**
- Replace: `frontend/apps/web-portal-e2e/src/example.spec.ts` (Nx-generated stub) → `frontend/apps/web-portal-e2e/src/smoke.spec.ts`

**Rationale:** Asserts the dev server boots, the shell renders, locale switch flips ar↔en + `dir` attribute, axe-core sees zero blocking violations.

- [ ] **Step 1: Remove the Nx-generated stub spec**

```bash
rm -f frontend/apps/web-portal-e2e/src/example.spec.ts
```

- [ ] **Step 2: Write `frontend/apps/web-portal-e2e/src/smoke.spec.ts`**

```typescript
import { test, expect } from '@playwright/test';
import { expectNoA11yViolations } from './support/axe';

test.describe('web-portal smoke', () => {
  test('renders root in Arabic with dir=rtl', async ({ page }, testInfo) => {
    await page.goto('/');
    await expect(page).toHaveURL(/\/health$/);
    const html = page.locator('html');
    await expect(html).toHaveAttribute('dir', 'rtl');
    await expect(html).toHaveAttribute('lang', 'ar');
    await expectNoA11yViolations(page, testInfo);
  });

  test('locale switcher toggles ar→en and flips dir to ltr', async ({ page }, testInfo) => {
    await page.goto('/');
    const html = page.locator('html');
    await expect(html).toHaveAttribute('dir', 'rtl');
    const switcher = page.getByRole('button', { name: /English|switchTo/i });
    await switcher.click();
    await expect(html).toHaveAttribute('dir', 'ltr');
    await expect(html).toHaveAttribute('lang', 'en');
    await expectNoA11yViolations(page, testInfo);
  });

  test('/health page renders status from External API', async ({ page }, testInfo) => {
    await page.goto('/health');
    // Page may show loading first; wait for status text
    await expect(page.locator('dl, [role="alert"]').first()).toBeVisible({ timeout: 10_000 });
    // If the External API isn't running, the page shows an error — assert one of the two outcomes
    const ok = await page.locator('text=ok').first().isVisible().catch(() => false);
    const err = await page.locator('[role="alert"]').first().isVisible().catch(() => false);
    expect(ok || err).toBe(true);
    await expectNoA11yViolations(page, testInfo);
  });
});
```

The third test deliberately accepts EITHER `ok` rendered OR `[role="alert"]` (server error) — Foundation's E2E doesn't strictly require the External API to be running. Phase 16 CI starts the API explicitly before E2E.

- [ ] **Step 3: Commit**

```bash
git add frontend/apps/web-portal-e2e/src/
git -c commit.gpgsign=false commit -m "feat(phase-14): web-portal-e2e smoke specs (root, locale switch, health page) with axe-core a11y"
```

---

## Task 14.3: admin-cms smoke E2E — root redirects to Keycloak

**Files:**
- Replace: `frontend/apps/admin-cms-e2e/src/example.spec.ts` (Nx-generated stub) → `frontend/apps/admin-cms-e2e/src/smoke.spec.ts`

**Rationale:** Admin app is auth-gated. Foundation E2E asserts the OIDC redirect happens (URL changes to `localhost:8080`) without driving through the full Keycloak login UI (that requires real credentials + form interaction; defer to Phase 18 manual verification).

- [ ] **Step 1: Remove stub**

```bash
rm -f frontend/apps/admin-cms-e2e/src/example.spec.ts
```

- [ ] **Step 2: Write `frontend/apps/admin-cms-e2e/src/smoke.spec.ts`**

```typescript
import { test, expect } from '@playwright/test';
import { expectNoA11yViolations } from './support/axe';

test.describe('admin-cms smoke', () => {
  test('renders shell with sign-in CTA before login', async ({ page }, testInfo) => {
    await page.goto('/');
    // Auto-login guard may immediately redirect; pre-redirect render also works.
    // Assert one of: sign-in button visible OR URL is on Keycloak realm.
    const onKeycloak = page.url().includes('/realms/cce-internal');
    if (!onKeycloak) {
      const signIn = page.getByRole('button', { name: /sign in|تسجيل الدخول/i });
      await expect(signIn).toBeVisible({ timeout: 10_000 });
      await expectNoA11yViolations(page, testInfo);
    } else {
      // Already redirected; just verify we're on Keycloak. No axe-core on third-party page.
      await expect(page).toHaveURL(/\/realms\/cce-internal/);
    }
  });

  test('clicking sign-in redirects to Keycloak realm', async ({ page }) => {
    await page.goto('/');
    if (page.url().includes('/realms/cce-internal')) {
      // Already redirected by autoLoginPartialRoutesGuard
      await expect(page).toHaveURL(/\/realms\/cce-internal/);
      return;
    }
    const signIn = page.getByRole('button', { name: /sign in|تسجيل الدخول/i });
    await signIn.click();
    await page.waitForURL(/\/realms\/cce-internal/, { timeout: 15_000 });
    await expect(page).toHaveURL(/\/realms\/cce-internal\/protocol\/openid-connect\/auth/);
  });
});
```

- [ ] **Step 3: Commit**

```bash
git add frontend/apps/admin-cms-e2e/src/
git -c commit.gpgsign=false commit -m "feat(phase-14): admin-cms-e2e smoke specs (shell render, sign-in → Keycloak redirect) with axe-core"
```

---

## Task 14.4: Configure Playwright `webServer` to auto-start backend + frontend

**Files:**
- Modify: `frontend/apps/web-portal-e2e/playwright.config.ts`
- Modify: `frontend/apps/admin-cms-e2e/playwright.config.ts`

**Rationale:** Nx-generated Playwright config auto-starts the matching dev server (via `webServer:` field). Foundation also wants the External API (web-portal e2e) and Internal API + live Keycloak (admin-cms e2e) running. We don't `webServer`-spawn the .NET APIs from Playwright (it's awkward) — instead we document that the dev API processes must be running OR that the test gracefully accepts an error response (which Task 14.2's third test already does).

For Foundation, just verify the existing Nx-generated webServer block points at the right Angular app.

- [ ] **Step 1: Inspect both Playwright configs**

```bash
grep -A6 webServer frontend/apps/web-portal-e2e/playwright.config.ts
grep -A6 webServer frontend/apps/admin-cms-e2e/playwright.config.ts
```
Expected: each has a `webServer.command` that invokes `pnpm exec nx run <app>:serve` (or similar) on the right port.

If admin-cms-e2e's port doesn't match `4201`, fix it. Same for web-portal-e2e + 4200.

- [ ] **Step 2: (No commit unless config edits were needed)**

If both configs match expectations, skip the commit. Otherwise commit the port fixes:

```bash
git add frontend/apps/*-e2e/playwright.config.ts
git -c commit.gpgsign=false commit -m "fix(phase-14): align Playwright webServer ports to web-portal:4200 and admin-cms:4201"
```

---

## Task 14.5: Run E2E + commit (no new files)

**Files:** None — verification only.

- [ ] **Step 1: Ensure backend APIs are running for web-portal E2E**

```bash
# In a separate shell or background, before running E2E
cd backend && dotnet run --project src/CCE.Api.External --urls http://localhost:5001 > /tmp/api-external.log 2>&1 &
EXT_PID=$!
sleep 4
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5001/health   # should be 200
```

- [ ] **Step 2: Run web-portal E2E**

```bash
cd frontend
pnpm nx e2e web-portal-e2e --reporter=list 2>&1 | tail -25
cd ..
```
Expected: 3 tests pass.

If a11y violations fire, Phase 14's axe scope hit a real issue — DO NOT just suppress; fix the offending template (it's our shell from Phase 11).

- [ ] **Step 3: Run admin-cms E2E**

```bash
cd frontend
pnpm nx e2e admin-cms-e2e --reporter=list 2>&1 | tail -25
cd ..
```
Expected: 2 tests pass.

- [ ] **Step 4: Stop the External API**

```bash
kill $EXT_PID 2>/dev/null; wait $EXT_PID 2>/dev/null
```

- [ ] **Step 5: (No commit — verification only)**

---

## Phase 14 — completion checklist

- [ ] `@axe-core/playwright` installed.
- [ ] Per-app `support/axe.ts` helper exports `expectNoA11yViolations(page, testInfo, scope?)`.
- [ ] `web-portal-e2e/smoke.spec.ts` has 3 tests; all pass against running External API.
- [ ] `admin-cms-e2e/smoke.spec.ts` has 2 tests covering shell render + Keycloak redirect.
- [ ] Playwright config points each app's webServer at 4200 / 4201 respectively.
- [ ] `pnpm nx e2e web-portal-e2e` green.
- [ ] `pnpm nx e2e admin-cms-e2e` green.
- [ ] `git status` clean.
- [ ] ~3–4 new commits.

**If all boxes ticked, phase 14 is complete. Proceed to phase 15 (k6 load tests).**
