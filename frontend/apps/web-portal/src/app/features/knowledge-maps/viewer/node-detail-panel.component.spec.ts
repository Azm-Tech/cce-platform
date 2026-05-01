import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import type { KnowledgeMapEdge, KnowledgeMapNode } from '../knowledge-maps.types';
import { NodeDetailPanelComponent } from './node-detail-panel.component';

const NODE_A: KnowledgeMapNode = {
  id: 'a', mapId: 'm1',
  nameAr: 'العقدة أ', nameEn: 'Node A',
  nodeType: 'Technology',
  descriptionAr: 'وصف أ', descriptionEn: 'Description A',
  iconUrl: null,
  layoutX: 0, layoutY: 0,
  orderIndex: 0,
};
const NODE_B: KnowledgeMapNode = {
  ...NODE_A, id: 'b', nameAr: 'العقدة ب', nameEn: 'Node B', nodeType: 'Sector',
  descriptionAr: null, descriptionEn: null,
};
const NODE_C: KnowledgeMapNode = { ...NODE_A, id: 'c', nameEn: 'Node C', nameAr: 'العقدة ج' };

const EDGE_AB: KnowledgeMapEdge = {
  id: 'eab', mapId: 'm1',
  fromNodeId: 'a', toNodeId: 'b',
  relationshipType: 'ParentOf',
  orderIndex: 0,
};
const EDGE_AC: KnowledgeMapEdge = { ...EDGE_AB, id: 'eac', toNodeId: 'c', relationshipType: 'RelatedTo' };

describe('NodeDetailPanelComponent', () => {
  let fixture: ComponentFixture<NodeDetailPanelComponent>;
  let component: NodeDetailPanelComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NodeDetailPanelComponent, TranslateModule.forRoot()],
      providers: [provideNoopAnimations()],
    }).compileComponents();

    fixture = TestBed.createComponent(NodeDetailPanelComponent);
    component = fixture.componentInstance;
  });

  it('renders nothing when node input is null', () => {
    fixture.componentRef.setInput('node', null);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.cce-node-detail')).toBeNull();
  });

  it('renders localized name + description when node is provided', () => {
    fixture.componentRef.setInput('node', NODE_A);
    fixture.componentRef.setInput('locale', 'en');
    fixture.detectChanges();
    expect(component.name()).toBe('Node A');
    expect(component.description()).toBe('Description A');
    const html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('Node A');
    expect(html).toContain('Description A');
  });

  it('locale toggle updates name and description', () => {
    fixture.componentRef.setInput('node', NODE_A);
    fixture.componentRef.setInput('locale', 'en');
    fixture.detectChanges();
    expect(component.name()).toBe('Node A');
    fixture.componentRef.setInput('locale', 'ar');
    fixture.detectChanges();
    expect(component.name()).toBe('العقدة أ');
    expect(component.description()).toBe('وصف أ');
  });

  it('renders "—" when description is null in the active locale', () => {
    fixture.componentRef.setInput('node', NODE_B);
    fixture.componentRef.setInput('locale', 'en');
    fixture.detectChanges();
    expect(component.description()).toBe('—');
  });

  it('outbound edges list renders one row per edge with the target localized name', () => {
    fixture.componentRef.setInput('node', NODE_A);
    fixture.componentRef.setInput('outboundEdges', [EDGE_AB, EDGE_AC]);
    fixture.componentRef.setInput('outboundTargets', [NODE_B, NODE_C]);
    fixture.componentRef.setInput('locale', 'en');
    fixture.detectChanges();
    const rows = fixture.nativeElement.querySelectorAll('.cce-node-detail__edge');
    expect(rows.length).toBe(2);
    const text = fixture.nativeElement.textContent ?? '';
    expect(text).toContain('Node B');
    expect(text).toContain('Node C');
  });

  it('clicking an edge button emits (nodeSelected) with the target id', () => {
    fixture.componentRef.setInput('node', NODE_A);
    fixture.componentRef.setInput('outboundEdges', [EDGE_AB]);
    fixture.componentRef.setInput('outboundTargets', [NODE_B]);
    fixture.detectChanges();
    let emitted: string | null = null;
    component.nodeSelected.subscribe((id) => { emitted = id; });
    const btn = fixture.nativeElement.querySelector('.cce-node-detail__edge') as HTMLButtonElement;
    btn.click();
    expect(emitted).toBe('b');
  });

  it('renders "no outbound connections" empty-state when outboundEdges is empty', () => {
    fixture.componentRef.setInput('node', NODE_A);
    fixture.componentRef.setInput('outboundEdges', []);
    fixture.detectChanges();
    expect(component.hasOutboundEdges()).toBe(false);
    const html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('knowledgeMaps.detail.noEdges');
  });

  it('ESC keydown emits (closed) when a node is visible', () => {
    fixture.componentRef.setInput('node', NODE_A);
    fixture.detectChanges();
    let closed = false;
    component.closed.subscribe(() => { closed = true; });
    component.onEscape();
    expect(closed).toBe(true);
  });

  it('ESC is a no-op when no node is visible', () => {
    fixture.componentRef.setInput('node', null);
    fixture.detectChanges();
    let closed = false;
    component.closed.subscribe(() => { closed = true; });
    component.onEscape();
    expect(closed).toBe(false);
  });

  it('close button click emits (closed)', () => {
    fixture.componentRef.setInput('node', NODE_A);
    fixture.detectChanges();
    let closed = false;
    component.closed.subscribe(() => { closed = true; });
    const btn = fixture.nativeElement.querySelector('.cce-node-detail__close') as HTMLButtonElement;
    btn.click();
    expect(closed).toBe(true);
  });
});
