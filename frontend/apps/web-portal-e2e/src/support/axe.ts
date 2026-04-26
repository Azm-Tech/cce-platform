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
