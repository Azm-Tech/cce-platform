import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { Subject } from 'rxjs';
import { MediaApiService } from '../../core/media/media-api.service';
import { CommunityApiService, type Result } from './community-api.service';
import { CommunityStateService } from './community-state.service';
import { ComposePostFormComponent } from './compose-post-form.component';
import {
  ComposePostDialogComponent,
  type ComposePostDialogData,
  type ComposePostDialogResult,
} from './compose-post-dialog.component';
import type { PublicTopic } from './community.types';

const VALID_CONTENT = 'This is content of at least ten characters.';
const VALID_TITLE = 'A valid post title';

const MOCK_TOPIC: PublicTopic = {
  id: 't1',
  nameAr: 'موضوع',
  nameEn: 'Topic',
  descriptionAr: null,
  descriptionEn: null,
  slug: 'topic',
  parentId: null,
  iconUrl: null,
  orderIndex: 0,
};

function ok<T>(value: T): Result<T> {
  return { ok: true, value };
}

// ── Form component tests (all logic lives here) ────────────────────────────────
describe('ComposePostFormComponent', () => {
  let fixture: ComponentFixture<ComposePostFormComponent>;
  let component: ComposePostFormComponent;
  let createPost: jest.Mock;
  let toastSuccess: jest.Mock;
  let uploadFileWithProgress: jest.Mock;
  let uploadSubject: Subject<any>;
  beforeAll(() => {
    if (typeof window !== 'undefined') {
      window.URL.createObjectURL = jest.fn().mockReturnValue('mock-url');
      window.URL.revokeObjectURL = jest.fn();
    }
  });
  async function setup(localeStart: 'ar' | 'en' = 'en') {
    createPost = jest.fn().mockResolvedValue(ok({ id: 'p2' }));
    toastSuccess = jest.fn();
    uploadSubject = new Subject<any>();
    uploadFileWithProgress = jest.fn().mockReturnValue(uploadSubject.asObservable());
    const localeSig = signal<'ar' | 'en'>(localeStart);

    await TestBed.configureTestingModule({
      imports: [
        ComposePostFormComponent,
        TranslocoTestingModule.forRoot({
          langs: { en: {}, ar: {} },
          translocoConfig: { availableLangs: ['en', 'ar'], defaultLang: 'en' },
        }),
      ],
      providers: [
        provideNoopAnimations(),
        { provide: CommunityApiService, useValue: { createPost } },
        { provide: CommunityStateService, useValue: { communityId: signal('c1') } },
        { provide: LocaleService, useValue: { locale: localeSig.asReadonly() } },
        { provide: ToastService, useValue: { success: toastSuccess, error: jest.fn() } },
        { provide: MediaApiService, useValue: { uploadFileWithProgress } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ComposePostFormComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('topics', [MOCK_TOPIC]);
    fixture.componentRef.setInput('preselectedTopicId', 't1');
    fixture.detectChanges();
  }

  it('valid submit posts payload with correct fields', async () => {
    await setup('en');
    component.form.patchValue({
      title: VALID_TITLE,
      content: VALID_CONTENT,
      locale: 'en',
      isAnswerable: false,
    });
    await component.triggerSubmit();
    expect(createPost).toHaveBeenCalledWith(
      expect.objectContaining({
        topicId: 't1',
        title: VALID_TITLE,
        content: VALID_CONTENT,
        locale: 'en',
        isAnswerable: false,
        saveAsDraft: false,
      }),
    );
  });

  it('content shorter than 10 chars makes form invalid (submit short-circuits)', async () => {
    await setup('en');
    component.form.patchValue({ title: VALID_TITLE, content: 'too short' });
    expect(component.form.invalid).toBe(true);
    createPost.mockClear();
    await component.triggerSubmit();
    expect(createPost).not.toHaveBeenCalled();
  });

  it('missing title makes form invalid (submit short-circuits)', async () => {
    await setup('en');
    component.form.patchValue({ title: '', content: VALID_CONTENT });
    expect(component.form.invalid).toBe(true);
    createPost.mockClear();
    await component.triggerSubmit();
    expect(createPost).not.toHaveBeenCalled();
  });

  it('post locale is derived from LocaleService.locale() (not a form input)', async () => {
    await setup('ar');
    expect(component.postLocale()).toBe('ar');
  });

  it('on success: toast.success + submitted event emitted with postId', async () => {
    await setup('en');
    const submittedSpy = jest.fn();
    component.submitted.subscribe(submittedSpy);
    component.form.patchValue({
      title: VALID_TITLE,
      content: VALID_CONTENT,
      locale: 'en',
      isAnswerable: true,
    });
    await component.triggerSubmit();
    expect(toastSuccess).toHaveBeenCalledWith('community.compose.toast');
    expect(submittedSpy).toHaveBeenCalledWith({ postId: 'p2' });
  });

  it('on error: errorKind signal set, submitted not emitted', async () => {
    await setup('en');
    createPost.mockResolvedValueOnce({ ok: false, error: { kind: 'server' } });
    const submittedSpy = jest.fn();
    component.submitted.subscribe(submittedSpy);
    component.form.patchValue({
      title: VALID_TITLE,
      content: VALID_CONTENT,
      locale: 'en',
      isAnswerable: true,
    });
    await component.triggerSubmit();
    expect(component.errorKind()).toBe('server');
    expect(submittedSpy).not.toHaveBeenCalled();
  });

  it('disables submit button while uploading media files and enables after success', async () => {
    await setup('en');
    component.form.patchValue({
      title: VALID_TITLE,
      content: VALID_CONTENT,
      locale: 'en',
    });

    const fileList = {
      length: 1,
      item: () => null,
      0: new File(['image-data'], 'image.png', { type: 'image/png' }),
    } as unknown as FileList;

    // Trigger handleFiles
    const handlePromise = (component as any).handleFiles(fileList);
    
    // Check uploading state
    expect(component.canSubmit()).toBe(false);

    // Complete upload successfully
    uploadSubject.next({ progress: 100, asset: { id: 'media-1', url: '/media/1' } });
    uploadSubject.complete();

    await handlePromise;

    expect(component.canSubmit()).toBe(true);
    expect(component.attachments()[0].status).toBe('success');
    expect(component.attachments()[0].assetFileId).toBe('media-1');
  });

  it('sets error state on attachments if media upload fails', async () => {
    await setup('en');
    const fileList = {
      length: 1,
      item: () => null,
      0: new File(['image-data'], 'image.png', { type: 'image/png' }),
    } as unknown as FileList;

    const handlePromise = (component as any).handleFiles(fileList);

    // Fail the upload
    uploadSubject.next({ progress: 0, error: { kind: 'server', message: 'error' } });
    uploadSubject.complete();

    await handlePromise;

    expect(component.attachments()[0].status).toBe('error');
    expect(component.attachments()[0].error).toBe('errors.server');
  });

  it('cancelUpload terminates subscription and removes the attachment', async () => {
    await setup('en');
    const fileList = {
      length: 1,
      item: () => null,
      0: new File(['image-data'], 'image.png', { type: 'image/png' }),
    } as unknown as FileList;

    const handlePromise = (component as any).handleFiles(fileList);

    const stagedId = component.attachments()[0].id;
    component.cancelUpload(stagedId);

    await handlePromise;

    expect(component.attachments()).toHaveLength(0);
  });
});

// ── Dialog shell smoke test ────────────────────────────────────────────────────
describe('ComposePostDialogComponent', () => {
  it('renders without error and closes with submitted: false on cancel', async () => {
    const dialogClose = jest.fn();
    const localeSig = signal<'ar' | 'en'>('en');

    await TestBed.configureTestingModule({
      imports: [
        ComposePostDialogComponent,
        TranslocoTestingModule.forRoot({
          langs: { en: {}, ar: {} },
          translocoConfig: { availableLangs: ['en', 'ar'], defaultLang: 'en' },
        }),
      ],
      providers: [
        provideNoopAnimations(),
        { provide: CommunityApiService, useValue: { createPost: jest.fn() } },
        { provide: CommunityStateService, useValue: { communityId: signal('c1') } },
        { provide: LocaleService, useValue: { locale: localeSig.asReadonly() } },
        { provide: ToastService, useValue: { success: jest.fn(), error: jest.fn() } },
        {
          provide: MatDialogRef,
          useValue: { close: dialogClose } as Partial<MatDialogRef<ComposePostDialogComponent, ComposePostDialogResult>>,
        },
        {
          provide: MAT_DIALOG_DATA,
          useValue: { topics: [MOCK_TOPIC], preselectedTopicId: 't1' } satisfies ComposePostDialogData,
        },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(ComposePostDialogComponent);
    fixture.detectChanges();
    fixture.componentInstance.cancel();
    expect(dialogClose).toHaveBeenCalledWith({ submitted: false });
  });
});
