import type { InteractiveMapNode } from '../knowledge-maps.types';
import { buildElements } from './elements';

const N1: InteractiveMapNode = {
  id: 'n1',
  nameAr: 'تقنية', nameEn: 'Technology',
  iconKey: 'tech',
  level: 1,
  parentId: null,
  topicId: 't1',
  tags: [],
};
const N2: InteractiveMapNode = {
  ...N1,
  id: 'n2',
  nameEn: 'Sector',
  level: 0,
  parentId: null,
};
const N3: InteractiveMapNode = {
  ...N1,
  id: 'n3',
  nameEn: 'Child',
  level: 2,
  parentId: 'n1',
};

describe('buildElements', () => {
  it('returns an empty array for empty inputs', () => {
    expect(buildElements([], { locale: 'en' })).toEqual([]);
  });

  it('maps each node to a Cytoscape element with data.id, label, level', () => {
    const out = buildElements([N1], { locale: 'en' });
    expect(out).toHaveLength(1);
    const e = out[0];
    expect(e.group).toBe('nodes');
    expect(e.data.id).toBe('n1');
    expect(e.data['label']).toBe('Technology');
    expect(e.data['level']).toBe(1);
  });

  it('selects nameAr vs nameEn based on the locale opt', () => {
    const en = buildElements([N1], { locale: 'en' });
    const ar = buildElements([N1], { locale: 'ar' });
    expect(en[0].data['label']).toBe('Technology');
    expect(ar[0].data['label']).toBe('تقنية');
  });

  it('does not add position fields (layout is computed by breadthfirst)', () => {
    const out = buildElements([N1], { locale: 'en' });
    expect(out[0].position).toBeUndefined();
  });

  it('derives an edge for each node with a parentId', () => {
    const out = buildElements([N1, N2, N3], { locale: 'en' });
    const edges = out.filter((e) => e.group === 'edges');
    expect(edges).toHaveLength(1);
    expect(edges[0].data['source']).toBe('n1');
    expect(edges[0].data['target']).toBe('n3');
  });

  it('does not create edges for nodes without parentId', () => {
    const out = buildElements([N1, N2], { locale: 'en' });
    const edges = out.filter((e) => e.group === 'edges');
    expect(edges).toHaveLength(0);
  });
});
