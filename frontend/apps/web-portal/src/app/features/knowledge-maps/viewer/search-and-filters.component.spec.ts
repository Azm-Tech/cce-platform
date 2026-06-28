import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { SearchAndFiltersComponent } from './search-and-filters.component';

describe('SearchAndFiltersComponent', () => {
  let fixture: ComponentFixture<SearchAndFiltersComponent>;
  let component: SearchAndFiltersComponent;

  beforeEach(async () => {
    jest.useFakeTimers();
    await TestBed.configureTestingModule({
      imports: [SearchAndFiltersComponent, TranslocoTestingModule.forRoot({ langs: { en: {}, ar: {} }, translocoConfig: { availableLangs: ['en', 'ar'], defaultLang: 'en' } })],
      providers: [provideNoopAnimations()],
    }).compileComponents();

    fixture = TestBed.createComponent(SearchAndFiltersComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  it('renders one chip per node level (3 chips: Root, Category, Topic)', () => {
    const chips = fixture.nativeElement.querySelectorAll('.cce-search-filters__chip');
    expect(chips.length).toBe(3);
  });

  it('typing in the search input emits (searchTermChange) after 200ms debounce', () => {
    let emitted: string | null = null;
    component.searchTermChange.subscribe((v) => { emitted = v; });
    component.onInput('carbon');
    // Before timer fires
    expect(emitted).toBeNull();
    jest.advanceTimersByTime(200);
    expect(emitted).toBe('carbon');
  });

  it('rapid typing only emits the final value once after debounce', () => {
    const emitted: string[] = [];
    component.searchTermChange.subscribe((v) => emitted.push(v));
    component.onInput('c');
    component.onInput('ca');
    component.onInput('car');
    jest.advanceTimersByTime(199);
    expect(emitted).toEqual([]);
    jest.advanceTimersByTime(1);
    expect(emitted).toEqual(['car']);
  });

  it('clicking an inactive chip emits (filtersChange) with the level added', () => {
    let emitted: ReadonlySet<number> | null = null;
    component.filtersChange.subscribe((v) => { emitted = v; });
    fixture.componentRef.setInput('filters', new Set<number>());
    component.toggleFilter(1);
    expect(emitted).not.toBeNull();
    expect(Array.from(emitted as unknown as Set<number>)).toEqual([1]);
  });

  it('clicking an active chip emits (filtersChange) with the level removed', () => {
    let emitted: ReadonlySet<number> | null = null;
    component.filtersChange.subscribe((v) => { emitted = v; });
    fixture.componentRef.setInput('filters', new Set<number>([0, 1]));
    component.toggleFilter(0);
    expect(Array.from(emitted as unknown as Set<number>)).toEqual([1]);
  });

  it('searchTerm input updates the visible input value via the effect', () => {
    fixture.componentRef.setInput('searchTerm', 'externally-set');
    fixture.detectChanges();
    expect(component.inputValue()).toBe('externally-set');
  });
});
