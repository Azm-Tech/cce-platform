import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { signal } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { TranslocoTestingModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { ToastService, provideCceIcons } from '@frontend/ui-kit';
import { CommunityApiService, type Result } from './community-api.service';
import { CommunityStateService } from './community-state.service';
import { AuthService } from '../../core/auth/auth.service';
import { FollowsApiService } from '../follows/follows-api.service';
import { FollowsStoreService } from '../follows/follows-store.service';
import type { PublicPost, PublicTopic } from './community.types';
import { TopicsListPage } from './topics-list.page';

const TRANSLOCO = TranslocoTestingModule.forRoot({
  langs: { en: {}, ar: {} },
  translocoConfig: { availableLangs: ['en', 'ar'], defaultLang: 'en' },
});

const COMMUNITY_ID = 'c1';

const T1: PublicTopic = {
  id: 't1', nameAr: 'موضوع 1', nameEn: 'Topic One',
  descriptionAr: 'وصف', descriptionEn: 'Desc 1',
  slug: 'one', parentId: null, iconUrl: null, orderIndex: 2,
};
const T2: PublicTopic = {
  ...T1, id: 't2', nameEn: 'Topic Two', nameAr: 'موضوع 2', slug: 'two', orderIndex: 1,
};

const P1: PublicPost = {
  id: 'p1', communityId: COMMUNITY_ID, topicId: 't1',
  type: 'Info',
  title: 'Hello world', content: 'Some content', locale: 'ar',
  author: { id: 'u1', name: 'Alice', avatarUrl: null, isExpert: false, postsCount: 0, followerCount: 0 },
  upvoteCount: 5, downvoteCount: 0, commentsCount: 3,
  answeredReplyId: null, isAnswerable: false,
  attachmentIds: [], createdOn: '2024-01-01T00:00:00Z',
  topicNameAr: null, topicNameEn: null,
  isWatchlisted: false, voteStatus: 0,
};

function ok<T>(value: T): Result<T> {
  return { ok: true, value };
}

describe('TopicsListPage', () => {
  let fixture: ComponentFixture<TopicsListPage>;
  let page: TopicsListPage;
  let api: { listTopics: jest.Mock; listFeedPosts: jest.Mock; getCommunityUser: jest.Mock; getCommunityLaws: jest.Mock };

  beforeEach(async () => {
    api = {
      listTopics: jest.fn().mockResolvedValue(ok([T1, T2])),
      listFeedPosts: jest.fn().mockResolvedValue(ok({ items: [P1], total: 1 })),
      getCommunityUser: jest.fn().mockResolvedValue({ ok: false, error: { kind: 'not_found' } }),
      getCommunityLaws: jest.fn().mockResolvedValue(ok([])),
    };

    await TestBed.configureTestingModule({
      imports: [TopicsListPage, TRANSLOCO],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: CommunityApiService, useValue: api },
        {
          provide: CommunityStateService,
          useValue: { ensureLoaded: jest.fn().mockResolvedValue(undefined), communityId: signal(COMMUNITY_ID) },
        },
        { provide: AuthService, useValue: { currentUser: signal(null), isAuthenticated: signal(false) } },
        { provide: LocaleService, useValue: { locale: signal('en') } },
        { provide: MatDialog, useValue: { open: jest.fn() } },
        { provide: ToastService, useValue: { success: jest.fn(), error: jest.fn() } },
        provideCceIcons(),
        FollowsStoreService,
        {
          provide: FollowsApiService,
          useValue: {
            follow: jest.fn(), unfollow: jest.fn(),
            getMyFollows: jest.fn().mockResolvedValue(ok({ topicIds: [], postIds: [], userIds: [] })),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TopicsListPage);
    page = fixture.componentInstance;
  });

  it('loadFeed fetches posts and populates posts signal', async () => {
    await page.loadFeed();
    expect(api.listFeedPosts).toHaveBeenCalledWith(
      COMMUNITY_ID,
      expect.objectContaining({ sort: '1' }),
    );
    expect(page.posts()).toHaveLength(1);
    expect(page.posts()[0].id).toBe('p1');
  });

  it('renders one cce-post-summary per post', async () => {
    await page.loadFeed();
    fixture.detectChanges();
    const cards = fixture.nativeElement.querySelectorAll('cce-post-summary');
    expect(cards).toHaveLength(1);
  });

  it('sets posts signal to empty array when feed returns no posts (NTF001)', async () => {
    api.listFeedPosts.mockResolvedValueOnce(ok({ items: [], total: 0 }));
    await page.loadFeed();
    expect(page.posts()).toHaveLength(0);
    expect(page.postsLoading()).toBe(false);
    expect(page.feedError()).toBeNull();
    expect(page.filteredPosts()).toHaveLength(0);
  });

  it('shows feedError when feed load fails (ERR001)', async () => {
    api.listFeedPosts.mockResolvedValueOnce({ ok: false, error: { kind: 'server' } });
    await page.loadFeed();
    expect(page.feedError()).toBe('server');
    expect(page.posts()).toHaveLength(0);
  });

  it('retry clears feedError and re-fetches feed', async () => {
    api.listFeedPosts.mockResolvedValueOnce({ ok: false, error: { kind: 'server' } });
    await page.loadFeed();
    expect(page.feedError()).toBe('server');

    api.listFeedPosts.mockResolvedValueOnce(ok({ items: [P1], total: 1 }));
    await page.loadFeed(); // same as retry — resets error, fetches again
    expect(page.feedError()).toBeNull();
    expect(page.posts()).toHaveLength(1);
  });

  it('setSort triggers a fresh feed load with the new sort value', async () => {
    await page.loadFeed();
    api.listFeedPosts.mockClear();
    api.listFeedPosts.mockResolvedValueOnce(ok({ items: [], total: 0 }));
    page.setSort(0);
    await fixture.whenStable();
    expect(api.listFeedPosts).toHaveBeenCalledWith(
      COMMUNITY_ID,
      expect.objectContaining({ sort: '0' }),
    );
  });

  it('setTopicFilter triggers a fresh feed load with the new topic filter', async () => {
    await page.loadFeed();
    api.listFeedPosts.mockClear();
    api.listFeedPosts.mockResolvedValueOnce(ok({ items: [], total: 0 }));
    page.setTopicFilter('t2');
    await fixture.whenStable();
    expect(api.listFeedPosts).toHaveBeenCalledWith(
      COMMUNITY_ID,
      expect.objectContaining({ topicId: 't2' }),
    );
  });
});
