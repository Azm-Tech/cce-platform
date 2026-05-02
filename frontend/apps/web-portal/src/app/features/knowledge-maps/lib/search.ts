import type { KnowledgeMapNode, NodeType } from '../knowledge-maps.types';

/**
 * Returns true when `node` should appear as a "match" given the
 * current search term + active type filters.
 *
 * Composition rules:
 * - Empty filter set means "no type filter" — every NodeType matches.
 * - Whitespace-only term means "no text search" — every name matches.
 * - Both filters active: AND semantics (must satisfy both).
 *
 * Search is case-insensitive substring against either localized name
 * (nameAr or nameEn). Locale-aware lowercase via toLocaleLowerCase()
 * so Arabic and Turkish-style edge cases behave correctly.
 */
export function nodeMatches(
  node: KnowledgeMapNode,
  term: string,
  filters: ReadonlySet<NodeType>,
): boolean {
  if (filters.size > 0 && !filters.has(node.nodeType)) return false;
  const trimmed = term.trim();
  if (!trimmed) return true;
  const needle = trimmed.toLocaleLowerCase();
  return (
    node.nameAr.toLocaleLowerCase().includes(needle) ||
    node.nameEn.toLocaleLowerCase().includes(needle)
  );
}
