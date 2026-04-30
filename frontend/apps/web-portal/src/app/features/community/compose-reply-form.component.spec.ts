import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { TranslateModule } from '@ngx-translate/core';
import { CommunityApiService, type Result } from './community-api.service';
import { ComposeReplyFormComponent } from './compose-reply-form.component';

function ok<T>(value: T): Result<T> {
  return { ok: true, value };
}

describe('ComposeReplyFormComponent', () => {
  let fixture: ComponentFixture<ComposeReplyFormComponent>;
  let component: ComposeReplyFormComponent;
  let createReply: jest.Mock;
  let toastSuccess: jest.Mock;
  let localeSig: ReturnType<typeof signal<'ar' | 'en'>>;
  let emittedReplyIds: string[];

  beforeEach(async () => {
    createReply = jest.fn().mockResolvedValue(ok({ id: 'r2' }));
    toastSuccess = jest.fn();
    localeSig = signal<'ar' | 'en'>('en');

    await TestBed.configureTestingModule({
      imports: [ComposeReplyFormComponent, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        { provide: CommunityApiService, useValue: { createReply } },
        { provide: LocaleService, useValue: { locale: localeSig.asReadonly() } },
        { provide: ToastService, useValue: { success: toastSuccess, error: jest.fn() } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(ComposeReplyFormComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('postId', 'p1');
    emittedReplyIds = [];
    component.replyCreated.subscribe((id) => emittedReplyIds.push(id));
    fixture.detectChanges();
  });

  it('valid submit calls createReply(postId, { content, locale })', async () => {
    component.form.patchValue({ content: 'My reply', locale: 'en' });
    await component.submit();
    expect(createReply).toHaveBeenCalledWith('p1', { content: 'My reply', locale: 'en' });
  });

  it('empty content makes form invalid; submit short-circuits', async () => {
    component.form.patchValue({ content: '' });
    expect(component.form.invalid).toBe(true);
    createReply.mockClear();
    await component.submit();
    expect(createReply).not.toHaveBeenCalled();
  });

  it('locale defaults to LocaleService.locale() at ngOnInit', async () => {
    // Re-create with ar locale.
    localeSig.set('ar');
    fixture = TestBed.createComponent(ComposeReplyFormComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('postId', 'p1');
    fixture.detectChanges();
    expect(component.form.controls.locale.value).toBe('ar');
  });

  it('on success: clears form, fires toast.success, emits replyCreated(id)', async () => {
    component.form.patchValue({ content: 'My reply', locale: 'en' });
    await component.submit();
    expect(component.form.controls.content.value).toBe('');
    expect(toastSuccess).toHaveBeenCalledWith('community.reply.toast');
    expect(emittedReplyIds).toEqual(['r2']);
  });

  it('on error: form keeps content, errorKind signal set, no emit', async () => {
    createReply.mockResolvedValueOnce({ ok: false, error: { kind: 'server' } });
    component.form.patchValue({ content: 'My reply', locale: 'en' });
    await component.submit();
    expect(component.errorKind()).toBe('server');
    expect(component.form.controls.content.value).toBe('My reply');
    expect(emittedReplyIds).toEqual([]);
  });
});
