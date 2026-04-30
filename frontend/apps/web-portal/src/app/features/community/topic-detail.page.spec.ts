import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { Subject } from 'rxjs';
import { MatDialog } from '@angular/material/dialog';
import { LocaleService } from '@frontend/i18n';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../../core/auth/auth.service';
import { CommunityApiService, type Result } from './community-api.service';
import type { PagedResult, PublicPost, PublicTopic } from './community.types';
import { TopicDetailPage } from './topic-detail.page';
import { FollowsApiService } from '../follows/follows-api.service';

const TOPIC: PublicTopic = {
  id: 't1',
  nameAr: 'موضوع', nameEn: 'Topic',
  descriptionAr: 'وصف', descriptionEn: 'Description',
  slug: 'one',
  parentId: null, iconUrl: null, orderIndex: 0,
};

const POST: PublicPost = {
  id: 'p1', topicId: 't1', authorId: 'u1',
  content: 'Hello', locale: 'en',
  isAnswerable: true,
  answeredReplyId: null,
  createdOn: '2026-04-29T12:00:00Z',
};

function ok<T>(value: T): Result<T> {
  return { ok: true, value };
}

describe('TopicDetailPage', () => {
  let fixture: ComponentFixture<TopicDetailPage>;
  let page: TopicDetailPage;
  let getTopicBySlug: jest.Mock;
  let listPosts: jest.Mock;
  let dialogOpen: jest.Mock;
  let afterClosed$: Subject<{ submitted: boolean; postId?: string }>;
  let isAuthenticatedSig: ReturnType<typeof signal<boolean>>;

  async function setup(slug: string | null = 'one') {
    getTopicBySlug = jest.fn().mockResolvedValue(ok(TOPIC));
    listPosts = jest.fn().mockResolvedValue(
      ok({ items: [POST], page: 1, pageSize: 20, total: 1 } as PagedResult<PublicPost>),
    );
    afterClosed$ = new Subject();
    dialogOpen = jest.fn().mockReturnValue({
      afterClosed: () => afterClosed$.asObservable(),
    });
    isAuthenticatedSig = signal<boolean>(true);
    const localeSig = signal<'ar' | 'en'>('en');

    await TestBed.configureTestingModule({
      imports: [TopicDetailPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: CommunityApiService, useValue: { getTopicBySlug, listPosts } },
        {
          provide: FollowsApiService,
          useValue: {
            getMyFollows: jest.fn().mockResolvedValue(ok({ topicIds: [], userIds: [], postIds: [] })),
            follow: jest.fn(),
            unfollow: jest.fn(),
          },
        },
        {
          provide: AuthService,
          useValue: { isAuthenticated: isAuthenticatedSig.asReadonly(), signIn: jest.fn() },
        },
        { provide: LocaleService, useValue: { locale: localeSig.asReadonly() } },
        { provide: MatDialog, useValue: { open: dialogOpen } },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: jest.fn(() => slug) } } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TopicDetailPage);
    page = fixture.componentInstance;
  }

  it('init: getTopicBySlug then listPosts called with the resolved id', async () => {
    await setup('one');
    fixture.detectChanges();
    await fixture.whenStable();
    expect(getTopicBySlug).toHaveBeenCalledWith('one');
    expect(listPosts).toHaveBeenCalledWith('t1', { page: 1, pageSize: 20 });
    expect(page.topic()?.id).toBe('t1');
    expect(page.posts()).toHaveLength(1);
  });

  it('404 on getTopicBySlug renders the not-found block', async () => {
    await setup('missing');
    getTopicBySlug.mockResolvedValueOnce({ ok: false, error: { kind: 'not-found' } });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.notFound()).toBe(true);
    expect(page.topic()).toBeNull();
  });

  it('topicName + topicDescription pull localized fields', async () => {
    await setup('one');
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.topicName()).toBe('Topic');
    expect(page.topicDescription()).toBe('Description');
  });

  it('paginator change re-fires listPosts with new page+pageSize', async () => {
    await setup('one');
    fixture.detectChanges();
    await fixture.whenStable();
    listPosts.mockClear();
    await page.onPage({ pageIndex: 1, pageSize: 50, length: 1, previousPageIndex: 0 });
    expect(page.page()).toBe(2);
    expect(page.pageSize()).toBe(50);
    expect(listPosts).toHaveBeenCalledWith('t1', { page: 2, pageSize: 50 });
  });

  it('openComposeDialog opens MatDialog with topic id payload', async () => {
    await setup('one');
    fixture.detectChanges();
    await fixture.whenStable();
    page.openComposeDialog();
    expect(dialogOpen).toHaveBeenCalled();
    const args = dialogOpen.mock.calls[0];
    expect(args[1].data).toEqual({ topicId: 't1' });
  });

  it('successful compose closes dialog with submitted -> reloads posts', async () => {
    await setup('one');
    fixture.detectChanges();
    await fixture.whenStable();
    page.openComposeDialog();
    listPosts.mockClear();
    afterClosed$.next({ submitted: true, postId: 'p2' });
    await Promise.resolve();
    expect(listPosts).toHaveBeenCalledWith('t1', { page: 1, pageSize: 20 });
  });
});
