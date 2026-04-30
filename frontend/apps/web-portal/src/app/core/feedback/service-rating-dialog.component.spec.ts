import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { TranslateModule } from '@ngx-translate/core';
import { ToastService } from '@frontend/ui-kit';
import { AccountApiService, type Result } from '../../features/account/account-api.service';
import {
  ServiceRatingDialogComponent,
  type ServiceRatingDialogData,
  type ServiceRatingDialogResult,
} from './service-rating-dialog.component';

describe('ServiceRatingDialogComponent', () => {
  let fixture: ComponentFixture<ServiceRatingDialogComponent>;
  let component: ServiceRatingDialogComponent;
  let submitServiceRating: jest.Mock;
  let dialogClose: jest.Mock;
  let toastSuccess: jest.Mock;

  function ok<T>(value: T): Result<T> {
    return { ok: true, value };
  }

  async function setup(data: ServiceRatingDialogData) {
    submitServiceRating = jest.fn().mockResolvedValue(ok({ id: 's1' }));
    dialogClose = jest.fn();
    toastSuccess = jest.fn();

    await TestBed.configureTestingModule({
      imports: [ServiceRatingDialogComponent, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        {
          provide: AccountApiService,
          useValue: { submitServiceRating },
        },
        { provide: ToastService, useValue: { success: toastSuccess, error: jest.fn() } },
        {
          provide: MatDialogRef,
          useValue: { close: dialogClose } as Partial<MatDialogRef<ServiceRatingDialogComponent, ServiceRatingDialogResult>>,
        },
        { provide: MAT_DIALOG_DATA, useValue: data },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ServiceRatingDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('5-star rating submit posts payload with rating=5 and the right page+locale', async () => {
    await setup({ page: 'home', locale: 'en' });
    component.setRating(5);
    component.comment.setValue('Great experience');
    await component.submit();
    expect(submitServiceRating).toHaveBeenCalledWith({
      rating: 5,
      commentAr: null,
      commentEn: 'Great experience',
      page: 'home',
      locale: 'en',
    });
  });

  it('comment populates commentAr when locale=ar (commentEn is null)', async () => {
    await setup({ page: 'kc', locale: 'ar' });
    component.setRating(4);
    component.comment.setValue('تجربة جيدة');
    await component.submit();
    expect(submitServiceRating).toHaveBeenCalledWith({
      rating: 4,
      commentAr: 'تجربة جيدة',
      commentEn: null,
      page: 'kc',
      locale: 'ar',
    });
  });

  it('on success: dialogRef.close({ submitted: true }) and toast.success fired', async () => {
    await setup({ page: 'home', locale: 'en' });
    component.setRating(3);
    await component.submit();
    expect(toastSuccess).toHaveBeenCalledWith('account.serviceRating.toast.thanks');
    expect(dialogClose).toHaveBeenCalledWith({ submitted: true });
  });

  it('on error: dialog stays open and errorKind signal is set', async () => {
    await setup({ page: 'home', locale: 'en' });
    submitServiceRating.mockResolvedValueOnce({ ok: false, error: { kind: 'server' } });
    component.setRating(2);
    await component.submit();
    expect(component.errorKind()).toBe('server');
    expect(dialogClose).not.toHaveBeenCalled();
  });

  it('canSubmit() requires rating in 1-5 (zero blocks)', async () => {
    await setup({ page: 'home', locale: 'en' });
    expect(component.canSubmit()).toBe(false);
    component.setRating(1);
    expect(component.canSubmit()).toBe(true);
  });
});
