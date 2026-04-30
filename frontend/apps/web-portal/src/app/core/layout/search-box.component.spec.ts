import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { SearchBoxComponent } from './search-box.component';

describe('SearchBoxComponent', () => {
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SearchBoxComponent, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    jest.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  it('creates the component', () => {
    const fixture = TestBed.createComponent(SearchBoxComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('starts with empty query', () => {
    const fixture = TestBed.createComponent(SearchBoxComponent);
    expect(fixture.componentInstance.query()).toBe('');
  });

  it('navigates to /search with query params on submit', () => {
    const fixture = TestBed.createComponent(SearchBoxComponent);
    const component = fixture.componentInstance;
    component.query.set('renewable energy');
    component.submit();
    expect(router.navigate).toHaveBeenCalledWith(
      ['/search'],
      { queryParams: { q: 'renewable energy' } },
    );
  });

  it('does not navigate when query is empty', () => {
    const fixture = TestBed.createComponent(SearchBoxComponent);
    const component = fixture.componentInstance;
    component.query.set('');
    component.submit();
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('does not navigate when query is only whitespace', () => {
    const fixture = TestBed.createComponent(SearchBoxComponent);
    const component = fixture.componentInstance;
    component.query.set('   ');
    component.submit();
    expect(router.navigate).not.toHaveBeenCalled();
  });
});
