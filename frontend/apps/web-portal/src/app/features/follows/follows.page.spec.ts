import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { TranslateModule } from '@ngx-translate/core';
import { ToastService } from '@frontend/ui-kit';
import { FollowsApiService, type Result } from './follows-api.service';
import type { MyFollows } from './follows.types';
import { FollowsPage } from './follows.page';

const SAMPLE: MyFollows = {
  topicIds: ['t1', 't2'],
  userIds: ['u1'],
  postIds: [],
};

describe('FollowsPage', () => {
  let fixture: ComponentFixture<FollowsPage>;
  let page: FollowsPage;
  let getMyFollows: jest.Mock;
  let unfollow: jest.Mock;
  let toastSuccess: jest.Mock;
  let toastError: jest.Mock;

  function ok<T>(value: T): Result<T> {
    return { ok: true, value };
  }

  beforeEach(async () => {
    getMyFollows = jest.fn().mockResolvedValue(ok(SAMPLE));
    unfollow = jest.fn().mockResolvedValue(ok(undefined));
    toastSuccess = jest.fn();
    toastError = jest.fn();

    await TestBed.configureTestingModule({
      imports: [FollowsPage, TranslateModule.forRoot()],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        {
          provide: FollowsApiService,
          useValue: { getMyFollows, follow: jest.fn(), unfollow },
        },
        { provide: ToastService, useValue: { success: toastSuccess, error: toastError } },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(FollowsPage);
    page = fixture.componentInstance;
  });

  it('init load renders three sections with correct chip counts', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(getMyFollows).toHaveBeenCalled();
    const sections = page.sections();
    expect(sections).toHaveLength(3);
    expect(sections[0].ids).toEqual(['t1', 't2']);
    expect(sections[1].ids).toEqual(['u1']);
    expect(sections[2].ids).toEqual([]);
  });

  it('all-empty result renders three empty messages', async () => {
    getMyFollows.mockResolvedValueOnce(ok({ topicIds: [], userIds: [], postIds: [] }));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    const html = fixture.nativeElement.textContent ?? '';
    expect(html).toContain('follows.empty.topics');
    expect(html).toContain('follows.empty.users');
    expect(html).toContain('follows.empty.posts');
  });

  it('unfollow click optimistically removes chip + calls unfollow + success toast', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    await page.unfollow('topic', 't1');
    expect(unfollow).toHaveBeenCalledWith('topic', 't1');
    expect(page.state()?.topicIds).toEqual(['t2']);
    expect(toastSuccess).toHaveBeenCalledWith('follows.unfollowToast');
  });

  it('on unfollow error, chip is re-inserted and error toast fires', async () => {
    unfollow.mockResolvedValueOnce({ ok: false, error: { kind: 'server' } });
    fixture.detectChanges();
    await fixture.whenStable();
    await page.unfollow('topic', 't1');
    expect(page.state()?.topicIds).toContain('t1');
    expect(toastError).toHaveBeenCalledWith('follows.errorToast');
    expect(toastSuccess).not.toHaveBeenCalled();
  });

  it('error path sets errorKind, retry triggers fresh fetch', async () => {
    getMyFollows.mockResolvedValueOnce({ ok: false, error: { kind: 'server' } });
    fixture.detectChanges();
    await fixture.whenStable();
    expect(page.errorKind()).toBe('server');
    getMyFollows.mockClear();
    getMyFollows.mockResolvedValueOnce(ok(SAMPLE));
    page.retry();
    await Promise.resolve();
    expect(getMyFollows).toHaveBeenCalled();
  });
});
