import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { FollowsApiService } from './follows-api.service';
import type { MyFollows } from './follows.types';

const SAMPLE: MyFollows = {
  topicIds: ['t1'],
  userIds: ['u1'],
  postIds: ['p1'],
};

describe('FollowsApiService', () => {
  let sut: FollowsApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(FollowsApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('getMyFollows GETs /api/me/follows', async () => {
    const promise = sut.getMyFollows();
    const req = http.expectOne('/api/me/follows');
    expect(req.request.method).toBe('GET');
    req.flush(SAMPLE);
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) expect(res.value).toEqual(SAMPLE);
  });

  it('follow("topic", id) POSTs to /api/me/follows/topics/{id}', async () => {
    const promise = sut.follow('topic', 't1');
    const req = http.expectOne('/api/me/follows/topics/t1');
    expect(req.request.method).toBe('POST');
    req.flush({});
    const res = await promise;
    expect(res.ok).toBe(true);
  });

  it('follow("user", id) POSTs to /api/me/follows/users/{id}', async () => {
    const promise = sut.follow('user', 'u1');
    const req = http.expectOne('/api/me/follows/users/u1');
    expect(req.request.method).toBe('POST');
    req.flush({});
    await promise;
  });

  it('follow("post", id) POSTs to /api/me/follows/posts/{id}', async () => {
    const promise = sut.follow('post', 'p1');
    const req = http.expectOne('/api/me/follows/posts/p1');
    expect(req.request.method).toBe('POST');
    req.flush({});
    await promise;
  });

  it('unfollow("topic", id) DELETEs /api/me/follows/topics/{id}', async () => {
    const promise = sut.unfollow('topic', 't1');
    const req = http.expectOne('/api/me/follows/topics/t1');
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });
    const res = await promise;
    expect(res.ok).toBe(true);
  });
});
