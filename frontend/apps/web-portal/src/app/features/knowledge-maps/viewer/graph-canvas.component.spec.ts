import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import * as cytoscapeLoader from '../lib/cytoscape-loader';
import type { KnowledgeMapEdge, KnowledgeMapNode } from '../knowledge-maps.types';
import { GraphCanvasComponent } from './graph-canvas.component';

const N1: KnowledgeMapNode = {
  id: 'n1', mapId: 'm1',
  nameAr: 'تقنية', nameEn: 'Tech',
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

/**
 * Builds a minimal Cytoscape `Core` stub that records the calls
 * GraphCanvasComponent makes against it. Returned from a mocked
 * mountCytoscape so jsdom never tries to render a real instance.
 */
function buildCyStub() {
  const tapHandlers: Array<(e: { target: { id: () => string } }) => void> = [];
  const selectHandlers: Array<() => void> = [];
  const selected = new Set<string>();
  const dimmedClasses = new Map<string, Set<string>>();

  const elements = () => ({
    remove: jest.fn(),
    unselect: jest.fn(() => selected.clear()),
  });

  const nodesCollection = {
    forEach: (fn: (n: { id: () => string; addClass: jest.Mock; removeClass: jest.Mock }) => void) => {
      [N1.id, N2.id].forEach((id) => {
        fn({
          id: () => id,
          addClass: jest.fn((cls: string) => {
            const set = dimmedClasses.get(id) ?? new Set();
            set.add(cls);
            dimmedClasses.set(id, set);
          }),
          removeClass: jest.fn((cls: string) => dimmedClasses.get(id)?.delete(cls)),
        });
      });
    },
    map: (fn: (n: { id: () => string }) => string) => Array.from(selected).map((id) => fn({ id: () => id })),
  };

  const edgesCollection = {
    forEach: (fn: (e: {
      source: () => { id: () => string };
      target: () => { id: () => string };
      addClass: jest.Mock;
      removeClass: jest.Mock;
    }) => void) => {
      fn({
        source: () => ({ id: () => 'n1' }),
        target: () => ({ id: () => 'n2' }),
        addClass: jest.fn(),
        removeClass: jest.fn(),
      });
    },
  };

  const cy = {
    on: jest.fn((event: string, _selector: string, handler: (e: { target: { id: () => string } }) => void) => {
      if (event === 'tap') tapHandlers.push(handler);
      if (event === 'select unselect') selectHandlers.push(handler as unknown as () => void);
    }),
    off: jest.fn(),
    elements,
    nodes: jest.fn((selector?: string) => {
      if (selector === ':selected') {
        return {
          map: (fn: (n: { id: () => string }) => string) =>
            Array.from(selected).map((id) => fn({ id: () => id })),
        };
      }
      return nodesCollection;
    }),
    edges: () => edgesCollection,
    add: jest.fn(),
    batch: jest.fn((fn: () => void) => fn()),
    zoom: jest.fn(() => 1),
    pan: jest.fn(() => ({ x: 0, y: 0 })),
    destroy: jest.fn(),
    $: jest.fn((sel: string) => {
      const id = sel.replace(/^#/, '').replace(/\\\\/g, '');
      return {
        length: 1,
        select: jest.fn(() => selected.add(id)),
      };
    }),
    fit: jest.fn(),
  };

  return {
    cy,
    tapHandlers,
    selectHandlers,
    selectedNodeIds: selected,
    dimmedClasses,
  };
}

describe('GraphCanvasComponent', () => {
  let fixture: ComponentFixture<GraphCanvasComponent>;
  let component: GraphCanvasComponent;
  let stub: ReturnType<typeof buildCyStub>;
  let mountSpy: jest.SpyInstance;

  beforeEach(async () => {
    stub = buildCyStub();
    mountSpy = jest
      .spyOn(cytoscapeLoader, 'mountCytoscape')
      .mockResolvedValue(stub.cy as never);

    await TestBed.configureTestingModule({
      imports: [GraphCanvasComponent],
      providers: [provideNoopAnimations()],
    }).compileComponents();

    fixture = TestBed.createComponent(GraphCanvasComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('nodes', [N1, N2]);
    fixture.componentRef.setInput('edges', [E1]);
  });

  afterEach(() => {
    mountSpy.mockRestore();
  });

  it('ngAfterViewInit calls mountCytoscape with elements built from inputs + the stylesheet', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(mountSpy).toHaveBeenCalledTimes(1);
    const opts = mountSpy.mock.calls[0][0];
    expect(opts.elements).toHaveLength(3); // 2 nodes + 1 edge
    expect(opts.style).toBeDefined();
    expect(opts.boxSelectionEnabled).toBe(true);
  });

  it('passes locale="en" labels (nameEn) to elements', async () => {
    fixture.componentRef.setInput('locale', 'en');
    fixture.detectChanges();
    await fixture.whenStable();
    const opts = mountSpy.mock.calls[0][0];
    const node1 = opts.elements.find((e: { data: { id: string } }) => e.data.id === 'n1');
    expect(node1.data.label).toBe('Tech');
  });

  it('passes locale="ar" labels (nameAr) and mirrored x-coordinates', async () => {
    fixture.componentRef.setInput('locale', 'ar');
    fixture.componentRef.setInput('mirrored', true);
    fixture.detectChanges();
    await fixture.whenStable();
    const opts = mountSpy.mock.calls[0][0];
    const node1 = opts.elements.find((e: { data: { id: string } }) => e.data.id === 'n1');
    expect(node1.data.label).toBe('تقنية');
    expect(node1.position.x).toBe(-100);
  });

  it('Cytoscape "tap" handler fires component nodeClick output with the tapped id', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    let emitted: string | null = null;
    component.nodeClick.subscribe((id) => { emitted = id; });
    expect(stub.tapHandlers).toHaveLength(1);
    stub.tapHandlers[0]({ target: { id: () => 'n2' } });
    expect(emitted).toBe('n2');
  });

  it('ngOnDestroy calls cy.destroy()', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.destroy();
    expect(stub.cy.destroy).toHaveBeenCalled();
  });
});
