import type { InteractiveMapNode } from '../knowledge-maps.types';

/**
 * Returns true when `node` should appear as a "match" given the
 * current search term + active level filters.
 *
 * Composition rules:
 * - Empty filter set means "no level filter" — every level matches.
 * - Whitespace-only term means "no text search" — every name matches.
 * - Both filters active: AND semantics (must satisfy both).
 *
 * Search is case-insensitive substring against either localized name
 * (nameAr or nameEn). Locale-aware lowercase via toLocaleLowerCase()
 * so Arabic and Turkish-style edge cases behave correctly.
 */
export function nodeMatches(
  node: InteractiveMapNode,
  term: string,
  filters: ReadonlySet<number>,
): boolean {
  if (filters.size > 0 && !filters.has(node.level)) return false;
  const trimmed = term.trim();
  if (!trimmed) return true;
  const needle = trimmed.toLocaleLowerCase();
  return (
    node.nameAr.toLocaleLowerCase().includes(needle) ||
    node.nameEn.toLocaleLowerCase().includes(needle)
  );
}
