import { TestBed } from '@angular/core/testing';
import { FollowsApiService, type Result } from './follows-api.service';
import { FollowsRegistryService } from './follows-registry.service';
import type { MyFollows } from './follows.types';

const SAMPLE: MyFollows = {
  topicIds: ['t1'],
  userIds: ['u1'],
  postIds: [],
};

function ok<T>(value: T): Result<T> {
  return { ok: true, value };
}

describe('FollowsRegistryService', () => {
  let sut: FollowsRegistryService;
  let getMyFollows: jest.Mock;

  beforeEach(() => {
    getMyFollows = jest.fn().mockResolvedValue(ok(SAMPLE));
    TestBed.configureTestingModule({
      providers: [
        FollowsRegistryService,
        {
          provide: FollowsApiService,
          useValue: { getMyFollows, follow: jest.fn(), unfollow: jest.fn() },
        },
      ],
    });
    sut = TestBed.inject(FollowsRegistryService);
  });

  it('ensureLoaded() calls getMyFollows() exactly once across N invocations', async () => {
    await Promise.all([sut.ensureLoaded(), sut.ensureLoaded(), sut.ensureLoaded()]);
    expect(getMyFollows).toHaveBeenCalledTimes(1);
    expect(sut.state()).toEqual(SAMPLE);
  });

  it('isFollowing returns true after load for entries in the cached state', async () => {
    await sut.ensureLoaded();
    expect(sut.isFollowing('topic', 't1')).toBe(true);
    expect(sut.isFollowing('topic', 'tX')).toBe(false);
    expect(sut.isFollowing('user', 'u1')).toBe(true);
    expect(sut.isFollowing('post', 'p1')).toBe(false);
  });

  it('setFollowing("topic", "tNew", true) appends the id to topicIds', async () => {
    await sut.ensureLoaded();
    sut.setFollowing('topic', 'tNew', true);
    expect(sut.state()?.topicIds).toContain('tNew');
    expect(sut.isFollowing('topic', 'tNew')).toBe(true);
  });

  it('setFollowing(type, id, false) removes the id from the cached state', async () => {
    await sut.ensureLoaded();
    sut.setFollowing('topic', 't1', false);
    expect(sut.state()?.topicIds).not.toContain('t1');
    expect(sut.isFollowing('topic', 't1')).toBe(false);
  });

  it('setFollowing is idempotent (no-op when already in target state)', async () => {
    await sut.ensureLoaded();
    const before = sut.state();
    sut.setFollowing('topic', 't1', true); // already true
    expect(sut.state()).toBe(before);
    sut.setFollowing('topic', 'tX', false); // already false
    expect(sut.state()).toBe(before);
  });
});
