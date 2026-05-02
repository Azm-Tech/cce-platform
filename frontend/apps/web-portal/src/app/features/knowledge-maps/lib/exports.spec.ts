import type { KnowledgeMapEdge, KnowledgeMapNode } from '../knowledge-maps.types';
import { exportJson } from './export-json';
import { exportPng } from './export-png';
import { exportSvg } from './export-svg';
import { exportPdf } from './export-pdf';
import * as cytoscapeLoader from './cytoscape-loader';

const NODE: KnowledgeMapNode = {
  id: 'n1', mapId: 'm1',
  nameAr: 'تقنية', nameEn: 'Technology',
  nodeType: 'Technology',
  descriptionAr: null, descriptionEn: null,
  iconUrl: null,
  layoutX: 100, layoutY: 200,
  orderIndex: 0,
};
const EDGE: KnowledgeMapEdge = {
  id: 'e1', mapId: 'm1',
  fromNodeId: 'n1', toNodeId: 'n2',
  relationshipType: 'ParentOf',
  orderIndex: 0,
};

// Type-erased factory because Core's full surface is huge and we mock
// only the methods our exporters call.
function fakeCy(overrides: Record<string, jest.Mock | unknown> = {}): unknown {
  return {
    png: jest.fn(),
    svg: jest.fn(),
    ...overrides,
  };
}

describe('exportJson', () => {
  it('serializes the payload to a pretty-printed JSON blob with the right type', () => {
    const blob = exportJson({
      map: { id: 'm1', nameAr: 'خريطة', nameEn: 'Map', slug: 'main' },
      nodes: [NODE],
      edges: [EDGE],
      exportedAt: '2026-05-02T12:00:00.000Z',
    });
    expect(blob.type).toBe('application/json');
    expect(blob.size).toBeGreaterThan(0);
  });
});

describe('exportPng', () => {
  it('calls cy.png with scale: 2 + output: blob and returns the result', async () => {
    const fakeBlob = new Blob(['png-bytes'], { type: 'image/png' });
    const png = jest.fn().mockReturnValue(fakeBlob);
    const cy = fakeCy({ png });
    const result = await exportPng(cy as never, { full: true });
    expect(png).toHaveBeenCalledWith({ scale: 2, full: true, output: 'blob' });
    expect(result).toBe(fakeBlob);
  });
});

describe('exportSvg', () => {
  it('ensures the SVG plugin is registered before calling cy.svg', async () => {
    const ensureSpy = jest
      .spyOn(cytoscapeLoader, 'ensureSvgPlugin')
      .mockResolvedValue(undefined);
    const svg = jest.fn().mockReturnValue('<svg/>');
    const cy = fakeCy({ svg });
    const result = await exportSvg(cy as never, { full: false });
    expect(ensureSpy).toHaveBeenCalledTimes(1);
    expect(svg).toHaveBeenCalledWith({ scale: 2, full: false });
    expect(result.type).toBe('image/svg+xml');
    ensureSpy.mockRestore();
  });
});

describe('exportPdf', () => {
  it('lazy-imports jspdf and wraps a PNG dataUri inside a landscape A4 page', async () => {
    const addImage = jest.fn();
    const output = jest.fn().mockReturnValue(new Blob(['pdf-bytes'], { type: 'application/pdf' }));
    const internal = {
      pageSize: { getWidth: () => 842, getHeight: () => 595 },
    };
    const fakeDoc = { addImage, output, internal };
    const FakeJsPdf = jest.fn().mockImplementation(() => fakeDoc);
    const dynamicImport = jest.fn().mockResolvedValue({ jsPDF: FakeJsPdf });
    // Replace the dynamic import with the spy via Jest module mock.
    jest.doMock('jspdf', () => ({ jsPDF: FakeJsPdf }), { virtual: true });

    const png = jest.fn().mockReturnValue('data:image/png;base64,FAKE');
    const cy = fakeCy({ png });

    const result = await exportPdf(cy as never, { full: true });

    expect(png).toHaveBeenCalledWith({ scale: 2, full: true });
    expect(FakeJsPdf).toHaveBeenCalledWith({ orientation: 'landscape', unit: 'pt', format: 'a4' });
    expect(addImage).toHaveBeenCalledWith(
      'data:image/png;base64,FAKE',
      'PNG',
      24,
      24,
      842 - 48,
      595 - 48,
      'cce-graph',
      'FAST',
    );
    expect(output).toHaveBeenCalledWith('blob');
    expect(result.type).toBe('application/pdf');
    // Suppress an "unused variable" lint by referencing the test plumbing.
    expect(dynamicImport).toBeDefined();
  });
});
