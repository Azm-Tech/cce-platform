import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { FilterRailComponent } from './filter-rail.component';

describe('FilterRailComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FilterRailComponent, TranslateModule.forRoot()],
      providers: [provideNoopAnimations()],
    }).compileComponents();
  });

  it('creates the component', () => {
    const fixture = TestBed.createComponent(FilterRailComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('has correct initial open state based on window width', () => {
    const fixture = TestBed.createComponent(FilterRailComponent);
    const component = fixture.componentInstance;
    // In jsdom (test environment), window.innerWidth is typically 1024
    const expectedOpen = typeof window !== 'undefined' && window.innerWidth > 768;
    expect(component.open()).toBe(expectedOpen);
  });

  it('toggles open state when toggle() is called', () => {
    const fixture = TestBed.createComponent(FilterRailComponent);
    const component = fixture.componentInstance;
    const initial = component.open();
    component.toggle();
    expect(component.open()).toBe(!initial);
  });

  it('toggles back to original state when toggle() called twice', () => {
    const fixture = TestBed.createComponent(FilterRailComponent);
    const component = fixture.componentInstance;
    const initial = component.open();
    component.toggle();
    component.toggle();
    expect(component.open()).toBe(initial);
  });
});
