import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import type { ViewerTab } from './map-viewer-store.service';
import { TabsBarComponent } from './tabs-bar.component';

const TAB1: ViewerTab = {
  id: 'm1',
  metadata: {
    id: 'm1',
    nameAr: 'الخريطة الأولى', nameEn: 'Map One',
    descriptionAr: '', descriptionEn: '',
    slug: 'one', isActive: true,
  },
  nodes: [],
  edges: [],
  loadedAt: 0,
};
const TAB2: ViewerTab = { ...TAB1, id: 'm2', metadata: { ...TAB1.metadata, id: 'm2', nameEn: 'Map Two', nameAr: 'الخريطة الثانية', slug: 'two' } };

describe('TabsBarComponent', () => {
  let fixture: ComponentFixture<TabsBarComponent>;
  let component: TabsBarComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TabsBarComponent, TranslateModule.forRoot()],
      providers: [provideNoopAnimations()],
    }).compileComponents();
    fixture = TestBed.createComponent(TabsBarComponent);
    component = fixture.componentInstance;
  });

  it('renders one button per tab', () => {
    fixture.componentRef.setInput('tabs', [TAB1, TAB2]);
    fixture.componentRef.setInput('activeId', 'm1');
    fixture.detectChanges();
    const tabs = fixture.nativeElement.querySelectorAll('.cce-tabs-bar__tab');
    expect(tabs.length).toBe(2);
  });

  it('renders nothing when tabs is empty', () => {
    fixture.componentRef.setInput('tabs', []);
    fixture.componentRef.setInput('activeId', null);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.cce-tabs-bar')).toBeNull();
  });

  it('active tab has the active class', () => {
    fixture.componentRef.setInput('tabs', [TAB1, TAB2]);
    fixture.componentRef.setInput('activeId', 'm2');
    fixture.detectChanges();
    const tabs = fixture.nativeElement.querySelectorAll('.cce-tabs-bar__tab');
    expect(tabs[0].classList.contains('cce-tabs-bar__tab--active')).toBe(false);
    expect(tabs[1].classList.contains('cce-tabs-bar__tab--active')).toBe(true);
  });

  it('locale toggle switches the visible label between nameAr and nameEn', () => {
    fixture.componentRef.setInput('tabs', [TAB1]);
    fixture.componentRef.setInput('activeId', 'm1');
    fixture.componentRef.setInput('locale', 'en');
    fixture.detectChanges();
    let html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('Map One');
    fixture.componentRef.setInput('locale', 'ar');
    fixture.detectChanges();
    html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('الخريطة الأولى');
  });

  it('clicking a non-active tab emits (tabSelected) with that tab id', () => {
    fixture.componentRef.setInput('tabs', [TAB1, TAB2]);
    fixture.componentRef.setInput('activeId', 'm1');
    fixture.detectChanges();
    let emitted: string | null = null;
    component.tabSelected.subscribe((id) => { emitted = id; });
    component.onSelect(TAB2);
    expect(emitted).toBe('m2');
  });

  it('clicking an already-active tab does not re-emit (tabSelected)', () => {
    fixture.componentRef.setInput('tabs', [TAB1]);
    fixture.componentRef.setInput('activeId', 'm1');
    fixture.detectChanges();
    let emitted: string | null = null;
    component.tabSelected.subscribe((id) => { emitted = id; });
    component.onSelect(TAB1);
    expect(emitted).toBeNull();
  });

  it('clicking the × emits (tabClosed) with that id (and stops propagation)', () => {
    fixture.componentRef.setInput('tabs', [TAB1]);
    fixture.componentRef.setInput('activeId', 'm1');
    fixture.detectChanges();
    let closed: string | null = null;
    let selected: string | null = null;
    component.tabClosed.subscribe((id) => { closed = id; });
    component.tabSelected.subscribe((id) => { selected = id; });
    const fakeEvent = { stopPropagation: jest.fn() } as unknown as Event;
    component.onClose(TAB1, fakeEvent);
    expect(fakeEvent.stopPropagation).toHaveBeenCalled();
    expect(closed).toBe('m1');
    expect(selected).toBeNull();
  });
});
