import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { TranslateModule } from '@ngx-translate/core';
import { CommunityApiService, type Result } from './community-api.service';
import {
  ComposePostDialogComponent,
  type ComposePostDialogData,
  type ComposePostDialogResult,
} from './compose-post-dialog.component';

const VALID_CONTENT = 'This is content of at least ten characters.';

function ok<T>(value: T): Result<T> {
  return { ok: true, value };
}

describe('ComposePostDialogComponent', () => {
  let fixture: ComponentFixture<ComposePostDialogComponent>;
  let component: ComposePostDialogComponent;
  let createPost: jest.Mock;
  let dialogClose: jest.Mock;
  let toastSuccess: jest.Mock;

  async function setup(localeStart: 'ar' | 'en' = 'en') {
    createPost = jest.fn().mockResolvedValue(ok({ id: 'p2' }));
    dialogClose = jest.fn();
    toastSuccess = jest.fn();
    const localeSig = signal<'ar' | 'en'>(localeStart);

    await TestBed.configureTestingModule({
      imports: [ComposePostDialogComponent, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        { provide: CommunityApiService, useValue: { createPost } },
        { provide: LocaleService, useValue: { locale: localeSig.asReadonly() } },
        { provide: ToastService, useValue: { success: toastSuccess, error: jest.fn() } },
        {
          provide: MatDialogRef,
          useValue: { close: dialogClose } as Partial<MatDialogRef<ComposePostDialogComponent, ComposePostDialogResult>>,
        },
        {
          provide: MAT_DIALOG_DATA,
          useValue: { topicId: 't1' } satisfies ComposePostDialogData,
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ComposePostDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('valid submit posts payload with topicId from dialog data', async () => {
    await setup('en');
    component.form.patchValue({ content: VALID_CONTENT, locale: 'en', isAnswerable: false });
    await component.submit();
    expect(createPost).toHaveBeenCalledWith({
      topicId: 't1',
      content: VALID_CONTENT,
      locale: 'en',
      isAnswerable: false,
    });
  });

  it('content shorter than 10 chars makes form invalid (submit short-circuits)', async () => {
    await setup('en');
    component.form.patchValue({ content: 'too short' });
    expect(component.form.invalid).toBe(true);
    createPost.mockClear();
    await component.submit();
    expect(createPost).not.toHaveBeenCalled();
  });

  it('locale defaults to LocaleService.locale() at construction time', async () => {
    await setup('ar');
    expect(component.form.controls.locale.value).toBe('ar');
  });

  it('on success: toast.success + dialogRef.close({ submitted: true, postId })', async () => {
    await setup('en');
    component.form.patchValue({ content: VALID_CONTENT, locale: 'en', isAnswerable: true });
    await component.submit();
    expect(toastSuccess).toHaveBeenCalledWith('community.compose.toast');
    expect(dialogClose).toHaveBeenCalledWith({ submitted: true, postId: 'p2' });
  });

  it('on error: dialog stays open, errorKind signal set, no close call', async () => {
    await setup('en');
    createPost.mockResolvedValueOnce({ ok: false, error: { kind: 'server' } });
    component.form.patchValue({ content: VALID_CONTENT, locale: 'en', isAnswerable: true });
    await component.submit();
    expect(component.errorKind()).toBe('server');
    expect(dialogClose).not.toHaveBeenCalled();
  });
});
