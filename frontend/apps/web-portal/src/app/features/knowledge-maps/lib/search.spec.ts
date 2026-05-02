import type { KnowledgeMapNode, NodeType } from '../knowledge-maps.types';
import { nodeMatches } from './search';

const N: KnowledgeMapNode = {
  id: 'n1', mapId: 'm1',
  nameAr: 'معالجة الكربون', nameEn: 'Carbon Capture',
  nodeType: 'Technology',
  descriptionAr: null, descriptionEn: null,
  iconUrl: null,
  layoutX: 0, layoutY: 0,
  orderIndex: 0,
};

describe('nodeMatches', () => {
  it('returns true for empty term + empty filters (everything matches)', () => {
    expect(nodeMatches(N, '', new Set())).toBe(true);
    expect(nodeMatches(N, '   ', new Set())).toBe(true); // whitespace-only
  });

  it('matches case-insensitive substring of nameEn', () => {
    expect(nodeMatches(N, 'carbon', new Set())).toBe(true);
    expect(nodeMatches(N, 'CARBON', new Set())).toBe(true);
    expect(nodeMatches(N, 'Capture', new Set())).toBe(true);
    expect(nodeMatches(N, 'storage', new Set())).toBe(false);
  });

  it('matches substring of nameAr', () => {
    expect(nodeMatches(N, 'الكربون', new Set())).toBe(true);
    expect(nodeMatches(N, 'معالجة', new Set())).toBe(true);
    expect(nodeMatches(N, 'بترول', new Set())).toBe(false);
  });

  it('filter set excludes nodes whose nodeType is not in the set', () => {
    const sectorOnly: ReadonlySet<NodeType> = new Set(['Sector']);
    expect(nodeMatches(N, '', sectorOnly)).toBe(false);
    const includesTech: ReadonlySet<NodeType> = new Set(['Technology', 'Sector']);
    expect(nodeMatches(N, '', includesTech)).toBe(true);
  });

  it('term + filter compose with AND semantics', () => {
    const techOnly: ReadonlySet<NodeType> = new Set(['Technology']);
    expect(nodeMatches(N, 'carbon', techOnly)).toBe(true); // both pass
    expect(nodeMatches(N, 'storage', techOnly)).toBe(false); // term fails
    const sectorOnly: ReadonlySet<NodeType> = new Set(['Sector']);
    expect(nodeMatches(N, 'carbon', sectorOnly)).toBe(false); // filter fails
  });
});
