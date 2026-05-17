import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { LocaleService } from '../locale.service';
import { LocaleSwitcherComponent } from './locale-switcher.component';

describe('LocaleSwitcherComponent', () => {
  let component: LocaleSwitcherComponent;
  let fixture: ComponentFixture<LocaleSwitcherComponent>;
  let locale: LocaleService;
  let translate: TranslocoService;

  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [LocaleSwitcherComponent, TranslocoModule.forRoot()],
      providers: [LocaleService],
    }).compileComponents();

    locale = TestBed.inject(LocaleService);
    translate = TestBed.inject(TranslocoService);
    jest.spyOn(translate, 'setActiveLang');
    fixture = TestBed.createComponent(LocaleSwitcherComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('switches locale + sets active lang on click', () => {
    component.toggle();
    expect(translate.setActiveLang).toHaveBeenCalledWith('en');
    expect(locale.locale()).toBe('en');
  });

  it('toggles back to ar after en', () => {
    component.toggle();
    component.toggle();
    expect(locale.locale()).toBe('ar');
  });
});
