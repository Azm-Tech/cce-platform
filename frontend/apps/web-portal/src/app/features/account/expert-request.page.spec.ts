import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { TranslateModule } from '@ngx-translate/core';
import { AccountApiService, type Result } from './account-api.service';
import type { ExpertRequestStatus } from './account.types';
import { ExpertRequestPage } from './expert-request.page';

const VALID_BIO_EN = 'A long enough English biography that satisfies the fifty character minimum length validator constraint.';
const VALID_BIO_AR = 'سيرة ذاتية طويلة بما يكفي لتلبية الحد الأدنى لطول النص المطلوب وهو خمسون حرفًا.';

const PENDING: ExpertRequestStatus = {
  id: 'e1',
  requestedById: 'u1',
  requestedBioAr: VALID_BIO_AR,
  requestedBioEn: VALID_BIO_EN,
  requestedTags: ['waste'],
  submittedOn: '2026-04-29T12:00:00Z',
  status: 'Pending',
  processedOn: null,
  rejectionReasonAr: null,
  rejectionReasonEn: null,
};

const REJECTED: ExpertRequestStatus = {
  ...PENDING,
  status: 'Rejected',
  processedOn: '2026-04-30T08:00:00Z',
  rejectionReasonAr: 'سبب الرفض',
  rejectionReasonEn: 'Rejection reason text',
};

describe('ExpertRequestPage', () => {
  let fixture: ComponentFixture<ExpertRequestPage>;
  let page: ExpertRequestPage;
  let getExpertStatus: jest.Mock;
  let submitExpertRequest: jest.Mock;
  let toastSuccess: jest.Mock;
  let localeSig: ReturnType<typeof signal<'ar' | 'en'>>;

  function ok<T>(value: T): Result<T> {
    return { ok: true, value };
  }

  beforeEach(async () => {
    getExpertStatus = jest.fn().mockResolvedValue(ok(null));
    submitExpertRequest = jest.fn().mockResolvedValue(ok(PENDING));
    toastSuccess = jest.fn();
    localeSig = signal<'ar' | 'en'>('en');

    await TestBed.configureTestingModule({
      imports: [ExpertRequestPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        {
          provide: AccountApiService,
          useValue: { getExpertStatus, submitExpertRequest },
        },
        { provide: LocaleService, useValue: { locale: localeSig.asReadonly() } },
        { provide: ToastService, useValue: { success: toastSuccess, error: jest.fn() } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(ExpertRequestPage);
    page = fixture.componentInstance;
  });

  it('with no request yet, renders the form (showForm=true) and no banner', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.status()).toBeNull();
    expect(page.showForm()).toBe(true);
  });

  it('with Pending status hides the form and renders the pending banner', async () => {
    getExpertStatus.mockResolvedValueOnce(ok(PENDING));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(page.status()?.status).toBe('Pending');
    expect(page.showForm()).toBe(false);
    const html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('account.expert.banner.pending');
  });

  it('with Rejected status renders banner with locale-aware reason + resubmit button reopens form', async () => {
    getExpertStatus.mockResolvedValueOnce(ok(REJECTED));
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.rejectionReason()).toBe('Rejection reason text');
    localeSig.set('ar');
    expect(page.rejectionReason()).toBe('سبب الرفض');
    expect(page.showForm()).toBe(false);
    page.resubmit();
    expect(page.showForm()).toBe(true);
  });

  it('valid submit posts payload and swaps to Pending banner with toast', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    page.form.patchValue({
      requestedBioAr: VALID_BIO_AR,
      requestedBioEn: VALID_BIO_EN,
      requestedTags: 'waste, water',
    });
    await page.submit();
    expect(submitExpertRequest).toHaveBeenCalledWith({
      requestedBioAr: VALID_BIO_AR,
      requestedBioEn: VALID_BIO_EN,
      requestedTags: ['waste', 'water'],
    });
    expect(page.status()).toEqual(PENDING);
    expect(page.showForm()).toBe(false);
    expect(toastSuccess).toHaveBeenCalledWith('account.expert.toast.submitted');
  });

  it('bio shorter than 50 chars makes the form invalid (submit short-circuits)', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    page.form.patchValue({
      requestedBioAr: 'short',
      requestedBioEn: 'too short',
      requestedTags: '',
    });
    expect(page.form.invalid).toBe(true);
    submitExpertRequest.mockClear();
    await page.submit();
    expect(submitExpertRequest).not.toHaveBeenCalled();
  });
});
