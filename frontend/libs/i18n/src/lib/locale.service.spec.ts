import { TestBed } from '@angular/core/testing';
import { LocaleService, SUPPORTED_LOCALES, type SupportedLocale } from './locale.service';

describe('LocaleService', () => {
  let service: LocaleService;

  beforeEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('dir');
    document.documentElement.removeAttribute('lang');
    TestBed.configureTestingModule({ providers: [LocaleService] });
    service = TestBed.inject(LocaleService);
  });

  it('defaults to ar when no preference stored', () => {
    expect(service.locale()).toBe('ar');
  });

  it('sets dir=rtl on html when locale is ar', () => {
    service.setLocale('ar');
    expect(document.documentElement.getAttribute('dir')).toBe('rtl');
    expect(document.documentElement.getAttribute('lang')).toBe('ar');
  });

  it('sets dir=ltr on html when locale is en', () => {
    service.setLocale('en');
    expect(document.documentElement.getAttribute('dir')).toBe('ltr');
    expect(document.documentElement.getAttribute('lang')).toBe('en');
  });

  it('persists locale to localStorage', () => {
    service.setLocale('en');
    expect(localStorage.getItem('cce.locale')).toBe('en');
  });

  it('reads persisted locale on instantiation', () => {
    localStorage.setItem('cce.locale', 'en');
    const fresh = TestBed.runInInjectionContext(() => new LocaleService());
    expect(fresh.locale()).toBe('en');
  });

  it('rejects unsupported locales (falls back to ar)', () => {
    service.setLocale('fr' as SupportedLocale);
    expect(service.locale()).toBe('ar');
  });

  it('exposes the supported locale list', () => {
    expect(SUPPORTED_LOCALES).toEqual(['ar', 'en']);
  });
});
