/**
 * Knowledge-map node icons. The registry + `iconDataUri` now live in the
 * shared ui-kit (so the admin icon picker renders the same set); this file
 * re-exports them for the existing in-feature imports (elements.ts, etc.).
 */
export { iconDataUri, isCustomIconUrl, MAP_ICON_KEYS } from '@frontend/ui-kit';
export type { IconDataUriOptions } from '@frontend/ui-kit';
