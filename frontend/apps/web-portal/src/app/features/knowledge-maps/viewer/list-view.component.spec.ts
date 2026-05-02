import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import type { KnowledgeMapEdge, KnowledgeMapNode } from '../knowledge-maps.types';
import { ListViewComponent } from './list-view.component';

const N1: KnowledgeMapNode = {
  id: 'n1', mapId: 'm1',
  nameAr: 'تقنية', nameEn: 'Technology One',
  nodeType: 'Technology',
  descriptionAr: null, descriptionEn: null,
  iconUrl: null,
  layoutX: 0, layoutY: 0,
  orderIndex: 0,
};
const N2: KnowledgeMapNode = { ...N1, id: 'n2', nameEn: 'Technology Two', nameAr: 'تقنية ٢' };
const N3: KnowledgeMapNode = { ...N1, id: 'n3', nodeType: 'Sector', nameEn: 'Energy', nameAr: 'الطاقة' };
const N4: KnowledgeMapNode = { ...N1, id: 'n4', nodeType: 'SubTopic', nameEn: 'Carbon Capture', nameAr: 'احتجاز الكربون' };

const E1: KnowledgeMapEdge = {
  id: 'e1', mapId: 'm1',
  fromNodeId: 'n1', toNodeId: 'n2',
  relationshipType: 'ParentOf',
  orderIndex: 0,
};
const E2: KnowledgeMapEdge = { ...E1, id: 'e2', fromNodeId: 'n1', toNodeId: 'n3' };

describe('ListViewComponent', () => {
  let fixture: ComponentFixture<ListViewComponent>;
  let component: ListViewComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ListViewComponent, TranslateModule.forRoot()],
      providers: [provideNoopAnimations()],
    }).compileComponents();
    fixture = TestBed.createComponent(ListViewComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('nodes', [N1, N2, N3, N4]);
    fixture.componentRef.setInput('edges', [E1, E2]);
    fixture.detectChanges();
  });

  it('renders one section per NodeType (3 sections)', () => {
    const sections = fixture.nativeElement.querySelectorAll('.cce-list-view__section');
    expect(sections.length).toBe(3);
  });

  it('section count badges match the number of nodes of that type', () => {
    const groups = component.grouped();
    const tech = groups.find((g) => g.type === 'Technology');
    const sector = groups.find((g) => g.type === 'Sector');
    const subTopic = groups.find((g) => g.type === 'SubTopic');
    expect(tech?.nodes).toHaveLength(2);
    expect(sector?.nodes).toHaveLength(1);
    expect(subTopic?.nodes).toHaveLength(1);
  });

  it('locale toggle switches all node names between nameAr and nameEn', () => {
    fixture.componentRef.setInput('locale', 'en');
    fixture.detectChanges();
    let html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('Technology One');
    expect(html).toContain('Energy');
    expect(html).toContain('Carbon Capture');
    fixture.componentRef.setInput('locale', 'ar');
    fixture.detectChanges();
    html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('تقنية');
    expect(html).toContain('الطاقة');
    expect(html).toContain('احتجاز الكربون');
  });

  it('clicking a node emits (nodeSelected) with the node id', () => {
    let emitted: string | null = null;
    component.nodeSelected.subscribe((id) => { emitted = id; });
    component.onSelect(N3);
    expect(emitted).toBe('n3');
  });

  it('node matching selectedId has aria-current="true" and the selected class', () => {
    fixture.componentRef.setInput('selectedId', 'n2');
    fixture.detectChanges();
    const buttons = fixture.nativeElement.querySelectorAll('.cce-list-view__node');
    const selected = Array.from(buttons).filter((b) =>
      (b as HTMLElement).getAttribute('aria-current') === 'true',
    );
    expect(selected.length).toBe(1);
    expect((selected[0] as HTMLElement).classList.contains('cce-list-view__node--selected')).toBe(true);
  });

  it('nodes in dimmedIds get the dimmed class', () => {
    fixture.componentRef.setInput('dimmedIds', new Set(['n1', 'n3']));
    fixture.detectChanges();
    const dimmed = fixture.nativeElement.querySelectorAll('.cce-list-view__node--dimmed');
    expect(dimmed.length).toBe(2);
  });

  it('outbound edge counts are computed per source node', () => {
    expect(component.outboundCountOf(N1)).toBe(2); // E1 + E2 both originate at n1
    expect(component.outboundCountOf(N2)).toBe(0);
    expect(component.outboundCountOf(N3)).toBe(0);
  });
});
