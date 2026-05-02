import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../../core/auth/auth.service';
import { InteractiveCityApiService } from '../interactive-city-api.service';
import { ScenarioBuilderStore } from './scenario-builder-store.service';
import { ScenarioHeaderComponent } from './scenario-header.component';

describe('ScenarioHeaderComponent', () => {
  let fixture: ComponentFixture<ScenarioHeaderComponent>;
  let store: ScenarioBuilderStore;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ScenarioHeaderComponent, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        ScenarioBuilderStore,
        {
          provide: InteractiveCityApiService,
          useValue: {
            listTechnologies: jest.fn(),
            runScenario: jest.fn(),
            listMyScenarios: jest.fn(),
            saveScenario: jest.fn(),
            deleteMyScenario: jest.fn(),
          },
        },
        { provide: AuthService, useValue: { isAuthenticated: signal<boolean>(false).asReadonly() } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(ScenarioHeaderComponent);
    store = fixture.debugElement.injector.get(ScenarioBuilderStore);
  });

  it('renders three input fields with translated labels', () => {
    fixture.detectChanges();
    const html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('interactiveCity.builder.name');
    expect(html).toContain('interactiveCity.builder.cityType');
    expect(html).toContain('interactiveCity.builder.targetYear');
  });

  it('writing to the name input updates store.name()', () => {
    fixture.detectChanges();
    const c = fixture.componentInstance.form.controls.name;
    c.setValue('Hello');
    expect(store.name()).toBe('Hello');
  });

  it('selecting a city-type updates store.cityType()', () => {
    fixture.detectChanges();
    fixture.componentInstance.form.controls.cityType.setValue('Industrial');
    expect(store.cityType()).toBe('Industrial');
  });

  it('editing the year input updates store.targetYear() with clamping', () => {
    fixture.detectChanges();
    const c = fixture.componentInstance.form.controls.targetYear;
    c.setValue(2040);
    expect(store.targetYear()).toBe(2040);
    // Below min — clamps to min (currentYear).
    const min = fixture.componentInstance.yearBounds.min;
    c.setValue(min - 100);
    expect(store.targetYear()).toBe(min);
  });

  it('store changes mirror into the form (URL hydrate path)', async () => {
    fixture.detectChanges();
    store.setName('From URL');
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.componentInstance.form.controls.name.value).toBe('From URL');
  });
});
