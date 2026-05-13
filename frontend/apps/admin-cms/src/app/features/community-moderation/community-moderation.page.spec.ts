import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { signal } from '@angular/core';
import { ConfirmDialogService, ToastService } from '@frontend/ui-kit';
import { EnvService } from '../../core/env.service';
import { CommunityModerationApiService } from './community-moderation-api.service';
import { CommunityModerationPage } from './community-moderation.page';
import type { AdminPostRow } from './admin-post.types';

const VALID = '11111111-1111-1111-1111-111111111111';

function mockPost(overrides: Partial<AdminPostRow> = {}): AdminPostRow {
  return {
    id: VALID,
    topicId: '22222222-2222-2222-2222-222222222222',
    topicNameEn: 'General',
    topicNameAr: 'عام',
    authorId: '33333333-3333-3333-3333-333333333333',
    content: 'Sample post body',
    locale: 'en',
    isAnswerable: false,
    isAnswered: false,
    isDeleted: false,
    createdOn: '2026-05-12T08:00:00Z',
    deletedOn: null,
    replyCount: 0,
    ...overrides,
  };
}

describe('CommunityModerationPage', () => {
  let fixture: ComponentFixture<CommunityModerationPage>;
  let page: CommunityModerationPage;
  let api: {
    listPosts: jest.Mock;
    softDeletePost: jest.Mock;
    softDeleteReply: jest.Mock;
    listTopicsLite: jest.Mock;
  };
  let confirm: { confirm: jest.Mock };
  let toast: { success: jest.Mock; error: jest.Mock };

  beforeEach(async () => {
    api = {
      listPosts: jest.fn().mockResolvedValue({
        ok: true,
        value: { items: [mockPost()], page: 1, pageSize: 20, total: 1 },
      }),
      softDeletePost: jest.fn().mockResolvedValue({ ok: true, value: undefined }),
      softDeleteReply: jest.fn().mockResolvedValue({ ok: true, value: undefined }),
      listTopicsLite: jest.fn().mockResolvedValue({ ok: true, value: [] }),
    };
    confirm = { confirm: jest.fn().mockResolvedValue(true) };
    toast = { success: jest.fn(), error: jest.fn() };

    await TestBed.configureTestingModule({
      imports: [CommunityModerationPage, TranslateModule.forRoot()],
      providers: [
        provideNoopAnimations(),
        { provide: CommunityModerationApiService, useValue: api },
        { provide: ConfirmDialogService, useValue: confirm },
        { provide: ToastService, useValue: toast },
        { provide: LocaleService, useValue: { locale: signal('en') } },
        {
          provide: EnvService,
          useValue: {
            env: {
              environment: 'development',
              apiBaseUrl: 'http://localhost:5002',
              oidcAuthority: 'https://login.microsoftonline.com/common/v2.0',
              oidcClientId: '00000000-0000-0000-0000-000000000000',
              sentryDsn: '',
              webPortalUrl: 'http://localhost:4200',
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CommunityModerationPage);
    page = fixture.componentInstance;
    fixture.detectChanges();
    // ngOnInit fires listPosts; let the microtask drain.
    await fixture.whenStable();
  });

  it('loads posts on init', () => {
    expect(api.listPosts).toHaveBeenCalled();
    expect(page.rows().length).toBe(1);
    expect(page.total()).toBe(1);
  });

  it('deletePost confirms then DELETEs + toasts on success', async () => {
    await page.deletePost(mockPost());
    expect(confirm.confirm).toHaveBeenCalled();
    expect(api.softDeletePost).toHaveBeenCalledWith(VALID);
    expect(toast.success).toHaveBeenCalledWith('communityModeration.post.toast');
  });

  it('skips deletePost when row is already deleted', async () => {
    await page.deletePost(mockPost({ isDeleted: true }));
    expect(api.softDeletePost).not.toHaveBeenCalled();
  });

  it('skips deletePost when confirm cancelled', async () => {
    confirm.confirm.mockResolvedValueOnce(false);
    await page.deletePost(mockPost());
    expect(api.softDeletePost).not.toHaveBeenCalled();
  });

  it('surfaces deletePost api error via toast.error', async () => {
    api.softDeletePost.mockResolvedValueOnce({ ok: false, error: { kind: 'forbidden' } });
    await page.deletePost(mockPost());
    expect(toast.error).toHaveBeenCalledWith('errors.forbidden');
  });

  it('deleteReplyById validates GUID', async () => {
    page.quickReplyId.set('not-a-guid');
    await page.deleteReplyById();
    expect(api.softDeleteReply).not.toHaveBeenCalled();
    expect(page.quickReplyError()).toBe(true);
  });

  it('deleteReplyById confirms + DELETEs on valid GUID', async () => {
    page.quickReplyId.set(VALID);
    await page.deleteReplyById();
    expect(api.softDeleteReply).toHaveBeenCalledWith(VALID);
    expect(toast.success).toHaveBeenCalledWith('communityModeration.reply.toast');
  });

  it('filter handlers re-trigger list load', () => {
    api.listPosts.mockClear();
    page.onStatus('deleted');
    expect(api.listPosts).toHaveBeenCalled();
    const lastCall = api.listPosts.mock.calls.at(-1)?.[0];
    expect(lastCall?.status).toBe('deleted');
  });

  it('publicPostUrl returns the deep-link to the web-portal', () => {
    const url = page.publicPostUrl(mockPost());
    expect(url).toBe(`http://localhost:4200/community/posts/${VALID}`);
  });

  it('openInPortal opens the deep-link in a new tab', () => {
    const spy = jest.spyOn(window, 'open').mockImplementation();
    try {
      page.openInPortal(mockPost());
      expect(spy).toHaveBeenCalledWith(
        `http://localhost:4200/community/posts/${VALID}`,
        '_blank',
        'noopener,noreferrer',
      );
    } finally {
      spy.mockRestore();
    }
  });

  it('openReplies appends #replies to the deep-link', () => {
    const spy = jest.spyOn(window, 'open').mockImplementation();
    try {
      page.openReplies(mockPost());
      expect(spy).toHaveBeenCalledWith(
        `http://localhost:4200/community/posts/${VALID}#replies`,
        '_blank',
        'noopener,noreferrer',
      );
    } finally {
      spy.mockRestore();
    }
  });

  it('copyId writes the post id to the clipboard + toasts', async () => {
    const writeText = jest.fn().mockResolvedValue(undefined);
    Object.defineProperty(navigator, 'clipboard', {
      value: { writeText },
      configurable: true,
    });
    await page.copyId(mockPost());
    expect(writeText).toHaveBeenCalledWith(VALID);
    expect(toast.success).toHaveBeenCalledWith(
      'communityModeration.action.copyIdToast',
    );
  });

  it('copyLink writes the public URL to the clipboard + toasts', async () => {
    const writeText = jest.fn().mockResolvedValue(undefined);
    Object.defineProperty(navigator, 'clipboard', {
      value: { writeText },
      configurable: true,
    });
    await page.copyLink(mockPost());
    expect(writeText).toHaveBeenCalledWith(
      `http://localhost:4200/community/posts/${VALID}`,
    );
    expect(toast.success).toHaveBeenCalledWith(
      'communityModeration.action.copyLinkToast',
    );
  });
});
