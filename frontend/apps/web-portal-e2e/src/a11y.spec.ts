import { test, expect } from '@playwright/test';
import { expectNoA11yViolations } from './support/axe';

/**
 * Sub-10a a11y CI gate. Asserts zero critical/serious axe-core findings
 * on the two production pages Sub-8 and Sub-9 deferred for retroactive
 * audit: /interactive-city (scenario builder) and /assistant (smart
 * assistant). Each page renders without backend coupling beyond the
 * already-public list endpoints.
 */
test.describe('Sub-10a a11y gate', () => {
  test('/interactive-city — axe clean', async ({ page }, testInfo) => {
    await page.goto('/interactive-city');
    // Wait for the catalog to render or for the empty state — either is
    // a valid "ready" state; the axe pass shouldn't depend on data load.
    await expect(page.locator('cce-scenario-builder-page')).toBeVisible({ timeout: 15_000 });
    await expectNoA11yViolations(page, testInfo);
  });

  test('/assistant — axe clean', async ({ page }, testInfo) => {
    await page.goto('/assistant');
    await expect(page.locator('cce-assistant-page')).toBeVisible({ timeout: 15_000 });
    // Compose box must be present — covers the "no thread yet" empty state.
    await expect(page.locator('cce-compose-box')).toBeVisible();
    await expectNoA11yViolations(page, testInfo);
  });
});
