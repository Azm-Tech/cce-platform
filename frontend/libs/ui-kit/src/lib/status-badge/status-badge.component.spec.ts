import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { StatusBadgeComponent, type StatusBadgeConfig } from './status-badge.component';

const CONFIG: StatusBadgeConfig = {
  approved: { tone: 'success', labelKey: 'x.approved' },
  pending: { tone: 'warning', labelKey: 'x.pending' },
};

describe('StatusBadgeComponent', () => {
  let fixture: ComponentFixture<StatusBadgeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        StatusBadgeComponent,
        TranslocoTestingModule.forRoot({
          langs: { en: { x: { approved: 'Approved', pending: 'Pending' } } },
          translocoConfig: { availableLangs: ['en'], defaultLang: 'en' },
        }),
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(StatusBadgeComponent);
  });

  function badge(): HTMLElement {
    return fixture.nativeElement.querySelector('.cce-status-badge');
  }

  it('renders the tone class + localized label for a known value', () => {
    fixture.componentRef.setInput('config', CONFIG);
    fixture.componentRef.setInput('value', 'approved');
    fixture.detectChanges();
    expect(badge().classList).toContain('cce-status-badge--success');
    expect(badge().textContent?.trim()).toBe('Approved');
  });

  it('matches the config case-insensitively', () => {
    fixture.componentRef.setInput('config', CONFIG);
    fixture.componentRef.setInput('value', 'Pending');
    fixture.detectChanges();
    expect(badge().classList).toContain('cce-status-badge--warning');
    expect(badge().textContent?.trim()).toBe('Pending');
  });

  it('falls back to a neutral badge showing the raw value when unknown', () => {
    fixture.componentRef.setInput('config', CONFIG);
    fixture.componentRef.setInput('value', 'archived');
    fixture.detectChanges();
    expect(badge().classList).toContain('cce-status-badge--neutral');
    expect(badge().textContent?.trim()).toBe('archived');
  });
});
