import type { KnowledgeMapEdge, KnowledgeMapNode } from '../knowledge-maps.types';
import { buildElements } from './elements';

const N1: KnowledgeMapNode = {
  id: 'n1', mapId: 'm1',
  nameAr: 'تقنية', nameEn: 'Technology',
  nodeType: 'Technology',
  descriptionAr: null, descriptionEn: null,
  iconUrl: null,
  layoutX: 100, layoutY: 200,
  orderIndex: 0,
};
const N2: KnowledgeMapNode = { ...N1, id: 'n2', nameEn: 'Sector', nodeType: 'Sector', layoutX: 300 };
const E1: KnowledgeMapEdge = {
  id: 'e1', mapId: 'm1',
  fromNodeId: 'n1', toNodeId: 'n2',
  relationshipType: 'ParentOf',
  orderIndex: 0,
};

describe('buildElements', () => {
  it('returns an empty array for empty inputs', () => {
    expect(buildElements([], [], { locale: 'en', mirrored: false })).toEqual([]);
  });

  it('maps each node to a Cytoscape element with data.id, label, nodeType', () => {
    const out = buildElements([N1], [], { locale: 'en', mirrored: false });
    expect(out).toHaveLength(1);
    const e = out[0];
    expect(e.group).toBe('nodes');
    expect(e.data.id).toBe('n1');
    expect(e.data['label']).toBe('Technology');
    expect(e.data['nodeType']).toBe('Technology');
    expect(e.position).toEqual({ x: 100, y: 200 });
  });

  it('selects nameAr vs nameEn based on the locale opt', () => {
    const en = buildElements([N1], [], { locale: 'en', mirrored: false });
    const ar = buildElements([N1], [], { locale: 'ar', mirrored: false });
    expect(en[0].data['label']).toBe('Technology');
    expect(ar[0].data['label']).toBe('تقنية');
  });

  it('mirrors layoutX (negates) when mirrored: true; leaves layoutY alone', () => {
    const out = buildElements([N1, N2], [], { locale: 'en', mirrored: true });
    expect(out[0].position).toEqual({ x: -100, y: 200 });
    expect(out[1].position).toEqual({ x: -300, y: 200 });
  });

  it('maps each edge to a Cytoscape element with source, target, relationshipType', () => {
    const out = buildElements([N1, N2], [E1], { locale: 'en', mirrored: false });
    const edge = out[2];
    expect(edge.group).toBe('edges');
    expect(edge.data.id).toBe('e1');
    expect(edge.data['source']).toBe('n1');
    expect(edge.data['target']).toBe('n2');
    expect(edge.data['relationshipType']).toBe('ParentOf');
  });
});
