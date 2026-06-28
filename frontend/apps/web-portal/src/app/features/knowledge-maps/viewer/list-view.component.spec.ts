import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslocoTestingModule } from '@jsverse/transloco';
import type { InteractiveMapNode } from '../knowledge-maps.types';
import { ListViewComponent } from './list-view.component';

const base: Omit<InteractiveMapNode, 'id' | 'nameEn' | 'nameAr' | 'level' | 'parentId'> = {
  iconKey: 'icon',
  topicId: 't1',
  tags: [],
};

const N1: InteractiveMapNode = {
  ...base, id: 'n1', nameEn: 'Technology One', nameAr: 'تقنية ١',
  level: 2, parentId: null,
};
const N2: InteractiveMapNode = {
  ...base, id: 'n2', nameEn: 'Technology Two', nameAr: 'تقنية ٢',
  level: 2, parentId: 'n1',
};
const N3: InteractiveMapNode = {
  ...base, id: 'n3', nameEn: 'Energy', nameAr: 'الطاقة',
  level: 1, parentId: 'n1',
};
const N4: InteractiveMapNode = {
  ...base, id: 'n4', nameEn: 'Carbon Capture', nameAr: 'احتجاز الكربون',
  level: 0, parentId: null,
};

describe('ListViewComponent', () => {
  let fixture: ComponentFixture<ListViewComponent>;
  let component: ListViewComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ListViewComponent, TranslocoTestingModule.forRoot({ langs: { en: {}, ar: {} }, translocoConfig: { availableLangs: ['en', 'ar'], defaultLang: 'en' } })],
      providers: [provideNoopAnimations()],
    }).compileComponents();
    fixture = TestBed.createComponent(ListViewComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('nodes', [N1, N2, N3, N4]);
    fixture.detectChanges();
  });

  it('renders one section per level (3 sections: 0, 1, 2)', () => {
    const sections = fixture.nativeElement.querySelectorAll('.cce-list-view__section');
    expect(sections.length).toBe(3);
  });

  it('groups nodes by level correctly', () => {
    const groups = component.grouped();
    const level0 = groups.find((g) => g.level === 0);
    const level1 = groups.find((g) => g.level === 1);
    const level2 = groups.find((g) => g.level === 2);
    expect(level0?.nodes).toHaveLength(1);
    expect(level1?.nodes).toHaveLength(1);
    expect(level2?.nodes).toHaveLength(2);
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
    expect(html).toContain('تقنية ١');
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

  it('childCountOf returns the number of direct children per node', () => {
    // N2.parentId='n1', N3.parentId='n1' → N1 has 2 children
    expect(component.childCountOf(N1)).toBe(2);
    expect(component.childCountOf(N2)).toBe(0);
    expect(component.childCountOf(N4)).toBe(0);
  });
});
