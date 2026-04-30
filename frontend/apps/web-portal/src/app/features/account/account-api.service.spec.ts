import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AccountApiService } from './account-api.service';
import type {
  ExpertRequestStatus,
  ServiceRatingPayload,
  SubmitExpertRequestPayload,
  UpdateMyProfilePayload,
  UserProfile,
} from './account.types';

const SAMPLE_PROFILE: UserProfile = {
  id: 'u1',
  email: 'jane@example.test',
  userName: 'jane',
  localePreference: 'en',
  knowledgeLevel: 'Beginner',
  interests: ['waste'],
  countryId: 'c1',
  avatarUrl: null,
};

const SAMPLE_EXPERT: ExpertRequestStatus = {
  id: 'e1',
  requestedById: 'u1',
  requestedBioAr: 'سيرة',
  requestedBioEn: 'Bio with at least fifty characters in length to satisfy validator.',
  requestedTags: ['waste'],
  submittedOn: '2026-04-29T12:00:00Z',
  status: 'Pending',
  processedOn: null,
  rejectionReasonAr: null,
  rejectionReasonEn: null,
};

describe('AccountApiService', () => {
  let sut: AccountApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(AccountApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('getProfile GETs /api/me', async () => {
    const promise = sut.getProfile();
    const req = http.expectOne('/api/me');
    expect(req.request.method).toBe('GET');
    req.flush(SAMPLE_PROFILE);
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) expect(res.value.email).toBe('jane@example.test');
  });

  it('getProfile returns not-found on 404', async () => {
    const promise = sut.getProfile();
    http.expectOne('/api/me').flush('', { status: 404, statusText: 'Not Found' });
    const res = await promise;
    expect(res.ok).toBe(false);
    if (!res.ok) expect(res.error.kind).toBe('not-found');
  });

  it('updateProfile PUTs /api/me with the payload body', async () => {
    const payload: UpdateMyProfilePayload = {
      localePreference: 'ar',
      knowledgeLevel: 'Intermediate',
      interests: ['carbon'],
      avatarUrl: 'https://example.test/a.png',
      countryId: 'c2',
    };
    const promise = sut.updateProfile(payload);
    const req = http.expectOne('/api/me');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(payload);
    req.flush({ ...SAMPLE_PROFILE, ...payload });
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) expect(res.value.knowledgeLevel).toBe('Intermediate');
  });

  it('getExpertStatus returns ok with null on 404 (no request yet)', async () => {
    const promise = sut.getExpertStatus();
    http
      .expectOne('/api/me/expert-status')
      .flush('', { status: 404, statusText: 'Not Found' });
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) expect(res.value).toBeNull();
  });

  it('getExpertStatus returns the DTO on 200', async () => {
    const promise = sut.getExpertStatus();
    http.expectOne('/api/me/expert-status').flush(SAMPLE_EXPERT);
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) {
      expect(res.value).not.toBeNull();
      expect(res.value?.status).toBe('Pending');
    }
  });

  it('submitExpertRequest POSTs /api/users/expert-request with the payload', async () => {
    const payload: SubmitExpertRequestPayload = {
      requestedBioAr: 'سيرة طويلة بما يكفي للتحقق',
      requestedBioEn: 'Long enough English biography to satisfy the server-side validator.',
      requestedTags: ['waste', 'water'],
    };
    const promise = sut.submitExpertRequest(payload);
    const req = http.expectOne('/api/users/expert-request');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    req.flush(SAMPLE_EXPERT);
    const res = await promise;
    expect(res.ok).toBe(true);
  });

  it('submitServiceRating POSTs /api/surveys/service-rating and returns the new id', async () => {
    const payload: ServiceRatingPayload = {
      rating: 5,
      commentEn: 'Great',
      commentAr: null,
      page: 'home',
      locale: 'en',
    };
    const promise = sut.submitServiceRating(payload);
    const req = http.expectOne('/api/surveys/service-rating');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);
    req.flush({ id: 's1' });
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) expect(res.value.id).toBe('s1');
  });
});
