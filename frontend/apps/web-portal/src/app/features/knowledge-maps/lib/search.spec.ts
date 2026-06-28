import type { InteractiveMapNode } from '../knowledge-maps.types';
import { nodeMatches } from './search';

const N: InteractiveMapNode = {
  id: 'n1',
  nameAr: 'معالجة الكربون', nameEn: 'Carbon Capture',
  iconKey: 'co2',
  level: 1,
  parentId: null,
  topicId: 't1',
  tags: [],
};

describe('nodeMatches', () => {
  it('returns true for empty term + empty filters (everything matches)', () => {
    expect(nodeMatches(N, '', new Set())).toBe(true);
    expect(nodeMatches(N, '   ', new Set())).toBe(true);
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

  it('filter set excludes nodes whose level is not in the set', () => {
    const level0Only: ReadonlySet<number> = new Set([0]);
    expect(nodeMatches(N, '', level0Only)).toBe(false); // N is level 1
    const level1Only: ReadonlySet<number> = new Set([1]);
    expect(nodeMatches(N, '', level1Only)).toBe(true);
  });

  it('term + filter compose with AND semantics', () => {
    const level1Only: ReadonlySet<number> = new Set([1]);
    expect(nodeMatches(N, 'carbon', level1Only)).toBe(true);
    expect(nodeMatches(N, 'storage', level1Only)).toBe(false); // term fails
    const level0Only: ReadonlySet<number> = new Set([0]);
    expect(nodeMatches(N, 'carbon', level0Only)).toBe(false); // filter fails
  });
});
