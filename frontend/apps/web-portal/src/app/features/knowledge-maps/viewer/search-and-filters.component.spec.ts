import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import type { NodeType } from '../knowledge-maps.types';
import { SearchAndFiltersComponent } from './search-and-filters.component';

describe('SearchAndFiltersComponent', () => {
  let fixture: ComponentFixture<SearchAndFiltersComponent>;
  let component: SearchAndFiltersComponent;

  beforeEach(async () => {
    jest.useFakeTimers();
    await TestBed.configureTestingModule({
      imports: [SearchAndFiltersComponent, TranslateModule.forRoot()],
      providers: [provideNoopAnimations()],
    }).compileComponents();

    fixture = TestBed.createComponent(SearchAndFiltersComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  it('renders one chip per NodeType (3 by default)', () => {
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

  it('clicking an inactive chip emits (filtersChange) with the type added', () => {
    let emitted: ReadonlySet<NodeType> | null = null;
    component.filtersChange.subscribe((v) => { emitted = v; });
    fixture.componentRef.setInput('filters', new Set<NodeType>());
    component.toggleFilter('Technology');
    expect(emitted).not.toBeNull();
    expect(Array.from(emitted as unknown as Set<NodeType>)).toEqual(['Technology']);
  });

  it('clicking an active chip emits (filtersChange) with the type removed', () => {
    let emitted: ReadonlySet<NodeType> | null = null;
    component.filtersChange.subscribe((v) => { emitted = v; });
    fixture.componentRef.setInput('filters', new Set<NodeType>(['Technology', 'Sector']));
    component.toggleFilter('Technology');
    expect(Array.from(emitted as unknown as Set<NodeType>)).toEqual(['Sector']);
  });

  it('searchTerm input updates the visible input value via the effect', () => {
    fixture.componentRef.setInput('searchTerm', 'externally-set');
    fixture.detectChanges();
    expect(component.inputValue()).toBe('externally-set');
  });
});
