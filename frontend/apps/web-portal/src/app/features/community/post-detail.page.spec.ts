import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService, type CurrentUser } from '../../core/auth/auth.service';
import { CommunityApiService, type Result } from './community-api.service';
import { FollowsApiService } from '../follows/follows-api.service';
import type { PagedResult, PublicPost, PublicPostReply } from './community.types';
import { PostDetailPage } from './post-detail.page';

const POST: PublicPost = {
  id: 'p1', topicId: 't1', authorId: 'u1',
  content: 'Question content', locale: 'en',
  isAnswerable: true,
  answeredReplyId: null,
  createdOn: '2026-04-29T12:00:00Z',
};

const R1: PublicPostReply = {
  id: 'r1', postId: 'p1', authorId: 'u2',
  content: 'first', locale: 'en',
  parentReplyId: null, isByExpert: false,
  createdOn: '2026-04-29T13:00:00Z',
};
const R2: PublicPostReply = { ...R1, id: 'r2', content: 'second', isByExpert: true };

const USER_AUTHOR: CurrentUser = {
  id: 'u1', email: 'a@b', userName: 'a',
  displayNameAr: null, displayNameEn: null,
  avatarUrl: null, countryId: null, isExpert: false,
};

function ok<T>(value: T): Result<T> {
  return { ok: true, value };
}

describe('PostDetailPage', () => {
  let fixture: ComponentFixture<PostDetailPage>;
  let page: PostDetailPage;
  let getPost: jest.Mock;
  let listReplies: jest.Mock;
  let isAuthSig: ReturnType<typeof signal<boolean>>;
  let currentUserSig: ReturnType<typeof signal<CurrentUser | null>>;

  async function setup(opts: { id?: string | null; user?: CurrentUser | null } = {}) {
    getPost = jest.fn().mockResolvedValue(ok(POST));
    listReplies = jest.fn().mockResolvedValue(
      ok({ items: [R1, R2], page: 1, pageSize: 20, total: 2 } as PagedResult<PublicPostReply>),
    );
    isAuthSig = signal<boolean>(opts.user !== null);
    currentUserSig = signal<CurrentUser | null>(opts.user ?? null);

    await TestBed.configureTestingModule({
      imports: [PostDetailPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: CommunityApiService, useValue: { getPost, listReplies, ratePost: jest.fn(), markAnswer: jest.fn(), createReply: jest.fn() } },
        {
          provide: FollowsApiService,
          useValue: {
            getMyFollows: jest.fn().mockResolvedValue(ok({ topicIds: [], userIds: [], postIds: [] })),
            follow: jest.fn(), unfollow: jest.fn(),
          },
        },
        {
          provide: AuthService,
          useValue: {
            isAuthenticated: isAuthSig.asReadonly(),
            currentUser: currentUserSig.asReadonly(),
            signIn: jest.fn(),
          },
        },
        { provide: LocaleService, useValue: { locale: signal<'ar' | 'en'>('en').asReadonly() } },
        { provide: ToastService, useValue: { success: jest.fn(), error: jest.fn() } },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: jest.fn(() => opts.id ?? 'p1') } } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PostDetailPage);
    page = fixture.componentInstance;
  }

  it('init: getPost + listReplies fired in parallel and bound to DOM', async () => {
    await setup({ user: USER_AUTHOR });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(getPost).toHaveBeenCalledWith('p1');
    expect(listReplies).toHaveBeenCalledWith('p1', { page: 1, pageSize: 20 });
    expect(page.post()).toEqual(POST);
    expect(page.replies()).toHaveLength(2);
  });

  it('404 on getPost renders not-found block', async () => {
    await setup({ user: USER_AUTHOR });
    getPost.mockResolvedValueOnce({ ok: false, error: { kind: 'not-found' } });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.notFound()).toBe(true);
  });

  it('accepted answer is hoisted to first position in orderedReplies', async () => {
    await setup({ user: USER_AUTHOR });
    getPost.mockResolvedValueOnce(ok({ ...POST, answeredReplyId: 'r2' }));
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.orderedReplies().map((r) => r.id)).toEqual(['r2', 'r1']);
  });

  it('canMarkAnswer is true when current user is the post author and post is answerable', async () => {
    await setup({ user: USER_AUTHOR });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.canMarkAnswer()).toBe(true);
  });

  it('canMarkAnswer is false for non-author', async () => {
    const otherUser: CurrentUser = { ...USER_AUTHOR, id: 'u-other' };
    await setup({ user: otherUser });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.canMarkAnswer()).toBe(false);
  });

  it('paginator change re-fires listReplies', async () => {
    await setup({ user: USER_AUTHOR });
    fixture.detectChanges();
    await fixture.whenStable();
    listReplies.mockClear();
    await page.onPage({ pageIndex: 1, pageSize: 50, length: 2, previousPageIndex: 0 });
    expect(listReplies).toHaveBeenCalledWith('p1', { page: 2, pageSize: 50 });
  });
});
