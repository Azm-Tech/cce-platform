import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { ConfirmDialogService, ToastService } from '@frontend/ui-kit';
import { TaxonomyApiService } from '../taxonomies/taxonomy-api.service';
import { CommunityModerationPage } from './community-moderation.page';

const VALID = '11111111-1111-1111-1111-111111111111';

describe('CommunityModerationPage', () => {
  let fixture: ComponentFixture<CommunityModerationPage>;
  let page: CommunityModerationPage;
  let softDeletePost: jest.Mock;
  let softDeleteReply: jest.Mock;
  let confirm: { confirm: jest.Mock };
  let toast: { success: jest.Mock; error: jest.Mock };

  beforeEach(async () => {
    softDeletePost = jest.fn().mockResolvedValue({ ok: true, value: undefined });
    softDeleteReply = jest.fn().mockResolvedValue({ ok: true, value: undefined });
    confirm = { confirm: jest.fn().mockResolvedValue(true) };
    toast = { success: jest.fn(), error: jest.fn() };

    await TestBed.configureTestingModule({
      imports: [CommunityModerationPage, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        { provide: TaxonomyApiService, useValue: { softDeletePost, softDeleteReply } },
        { provide: ConfirmDialogService, useValue: confirm },
        { provide: ToastService, useValue: toast },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(CommunityModerationPage);
    page = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('deletePost does nothing when GUID invalid', async () => {
    page.form.patchValue({ postId: 'not-a-guid' });
    await page.deletePost();
    expect(softDeletePost).not.toHaveBeenCalled();
  });

  it('deletePost confirms then DELETEs + toasts on success', async () => {
    page.form.patchValue({ postId: VALID });
    await page.deletePost();
    expect(confirm.confirm).toHaveBeenCalled();
    expect(softDeletePost).toHaveBeenCalledWith(VALID);
    expect(toast.success).toHaveBeenCalledWith('communityModeration.post.toast');
  });

  it('deleteReply confirms then DELETEs + toasts on success', async () => {
    page.form.patchValue({ replyId: VALID });
    await page.deleteReply();
    expect(softDeleteReply).toHaveBeenCalledWith(VALID);
    expect(toast.success).toHaveBeenCalledWith('communityModeration.reply.toast');
  });

  it('skips action when confirm cancelled', async () => {
    page.form.patchValue({ postId: VALID });
    confirm.confirm.mockResolvedValueOnce(false);
    await page.deletePost();
    expect(softDeletePost).not.toHaveBeenCalled();
  });

  it('surfaces api error via toast.error', async () => {
    page.form.patchValue({ postId: VALID });
    softDeletePost.mockResolvedValueOnce({ ok: false, error: { kind: 'forbidden' } });
    await page.deletePost();
    expect(toast.error).toHaveBeenCalledWith('errors.forbidden');
  });
});
