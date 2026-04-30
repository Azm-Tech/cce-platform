import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CommunityApiService } from './community-api.service';
import type {
  CreatePostPayload,
  CreateReplyPayload,
  PagedResult,
  PublicPost,
  PublicPostReply,
  PublicTopic,
} from './community.types';

const TOPIC: PublicTopic = {
  id: 't1',
  nameAr: 'اسم', nameEn: 'Name',
  descriptionAr: 'وصف', descriptionEn: 'Description',
  slug: 'slug',
  parentId: null,
  iconUrl: null,
  orderIndex: 0,
};

const POST: PublicPost = {
  id: 'p1', topicId: 't1', authorId: 'u1',
  content: 'Hello', locale: 'en',
  isAnswerable: true,
  answeredReplyId: null,
  createdOn: '2026-04-29T12:00:00Z',
};

const REPLY: PublicPostReply = {
  id: 'r1', postId: 'p1', authorId: 'u2',
  content: 'Reply', locale: 'en',
  parentReplyId: null,
  isByExpert: false,
  createdOn: '2026-04-29T13:00:00Z',
};

const PAGED_POSTS: PagedResult<PublicPost> = { items: [POST], page: 1, pageSize: 20, total: 1 };
const PAGED_REPLIES: PagedResult<PublicPostReply> = { items: [REPLY], page: 1, pageSize: 20, total: 1 };

describe('CommunityApiService', () => {
  let sut: CommunityApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(CommunityApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('listTopics GETs /api/topics', async () => {
    const promise = sut.listTopics();
    const req = http.expectOne('/api/topics');
    expect(req.request.method).toBe('GET');
    req.flush([TOPIC]);
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) expect(res.value).toEqual([TOPIC]);
  });

  it('getTopicBySlug returns not-found on 404', async () => {
    const promise = sut.getTopicBySlug('missing');
    http
      .expectOne('/api/community/topics/missing')
      .flush('', { status: 404, statusText: 'Not Found' });
    const res = await promise;
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.kind).toBe('not-found');
  });

  it('listPosts(topicId, { page }) GETs paged posts under topic', async () => {
    const promise = sut.listPosts('t1', { page: 2 });
    const req = http.expectOne((r) => r.url === '/api/community/topics/t1/posts');
    expect(req.request.params.get('page')).toBe('2');
    req.flush(PAGED_POSTS);
    await promise;
  });

  it('getPost GETs /api/community/posts/{id}', async () => {
    const promise = sut.getPost('p1');
    http.expectOne('/api/community/posts/p1').flush(POST);
    const res = await promise;
    expect(res.ok).toBe(true);
  });

  it('listReplies(postId, {}) GETs the replies endpoint', async () => {
    const promise = sut.listReplies('p1', {});
    http.expectOne((r) => r.url === '/api/community/posts/p1/replies').flush(PAGED_REPLIES);
    await promise;
  });

  it('createPost POSTs /api/community/posts with the payload + returns id', async () => {
    const payload: CreatePostPayload = {
      topicId: 't1', content: 'Hello', locale: 'en', isAnswerable: true,
    };
    const promise = sut.createPost(payload);
    const req = http.expectOne('/api/community/posts');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    req.flush({ id: 'p2' });
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) expect(res.value.id).toBe('p2');
  });

  it('createReply POSTs to /api/community/posts/{id}/replies', async () => {
    const payload: CreateReplyPayload = { content: 'r', locale: 'en' };
    const promise = sut.createReply('p1', payload);
    const req = http.expectOne('/api/community/posts/p1/replies');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    req.flush({ id: 'r2' });
    await promise;
  });

  it('ratePost POSTs { stars } to /api/community/posts/{id}/rate', async () => {
    const promise = sut.ratePost('p1', 5);
    const req = http.expectOne('/api/community/posts/p1/rate');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ stars: 5 });
    req.flush({});
    await promise;
  });

  it('markAnswer POSTs { replyId } to /api/community/posts/{id}/mark-answer', async () => {
    const promise = sut.markAnswer('p1', 'r1');
    const req = http.expectOne('/api/community/posts/p1/mark-answer');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ replyId: 'r1' });
    req.flush({});
    await promise;
  });

  it('editReply PUTs { content } to /api/community/replies/{id}', async () => {
    const promise = sut.editReply('r1', 'new content');
    const req = http.expectOne('/api/community/replies/r1');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ content: 'new content' });
    req.flush({});
    await promise;
  });
});
