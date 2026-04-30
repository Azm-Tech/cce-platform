import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { ToastService } from '@frontend/ui-kit';
import { ContentApiService } from './content-api.service';
import { CountryResourceRequestPage } from './country-resource-request.page';

const VALID_GUID = '11111111-1111-1111-1111-111111111111';

describe('CountryResourceRequestPage', () => {
  let fixture: ComponentFixture<CountryResourceRequestPage>;
  let page: CountryResourceRequestPage;
  let approveCountryResourceRequest: jest.Mock;
  let rejectCountryResourceRequest: jest.Mock;
  let toast: { success: jest.Mock; error: jest.Mock };

  beforeEach(async () => {
    approveCountryResourceRequest = jest.fn();
    rejectCountryResourceRequest = jest.fn();
    toast = { success: jest.fn(), error: jest.fn() };
    await TestBed.configureTestingModule({
      imports: [CountryResourceRequestPage, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        {
          provide: ContentApiService,
          useValue: { approveCountryResourceRequest, rejectCountryResourceRequest },
        },
        { provide: ToastService, useValue: toast },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(CountryResourceRequestPage);
    page = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('approve does nothing when requestId invalid', async () => {
    await page.approve();
    expect(approveCountryResourceRequest).not.toHaveBeenCalled();
  });

  it('approve POSTs (notes optional, sent as null when empty)', async () => {
    page.form.patchValue({ requestId: VALID_GUID });
    approveCountryResourceRequest.mockResolvedValueOnce({ ok: true, value: { id: VALID_GUID } });
    await page.approve();
    expect(approveCountryResourceRequest).toHaveBeenCalledWith(VALID_GUID, {
      adminNotesAr: null,
      adminNotesEn: null,
    });
    expect(toast.success).toHaveBeenCalledWith('countryResourceRequest.approve.toast');
  });

  it('reject requires both notes', async () => {
    page.form.patchValue({ requestId: VALID_GUID });
    await page.reject();
    expect(rejectCountryResourceRequest).not.toHaveBeenCalled();
    expect(toast.error).toHaveBeenCalledWith('countryResourceRequest.reject.notesRequired');
  });

  it('reject POSTs when notes provided', async () => {
    page.form.patchValue({
      requestId: VALID_GUID,
      adminNotesAr: 'سبب',
      adminNotesEn: 'reason',
    });
    rejectCountryResourceRequest.mockResolvedValueOnce({ ok: true, value: { id: VALID_GUID } });
    await page.reject();
    expect(rejectCountryResourceRequest).toHaveBeenCalledWith(VALID_GUID, {
      adminNotesAr: 'سبب',
      adminNotesEn: 'reason',
    });
    expect(toast.success).toHaveBeenCalledWith('countryResourceRequest.reject.toast');
  });

  it('approve surfaces error via toast.error', async () => {
    page.form.patchValue({ requestId: VALID_GUID });
    approveCountryResourceRequest.mockResolvedValueOnce({ ok: false, error: { kind: 'not-found' } });
    await page.approve();
    expect(toast.error).toHaveBeenCalledWith('errors.not-found');
  });
});
