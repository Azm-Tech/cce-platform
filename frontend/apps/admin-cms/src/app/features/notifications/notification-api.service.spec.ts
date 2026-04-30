import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { NotificationApiService } from './notification-api.service';

describe('NotificationApiService', () => {
  let sut: NotificationApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(NotificationApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('list builds channel + page query', async () => {
    const p = sut.list({ page: 1, pageSize: 20, channel: 'Email', isActive: true });
    const req = http.expectOne((r) => r.url === '/api/admin/notification-templates');
    expect(req.request.params.get('channel')).toBe('Email');
    expect(req.request.params.get('isActive')).toBe('true');
    req.flush({ items: [], page: 1, pageSize: 20, total: 0 });
    await p;
  });

  it('create POSTs body', async () => {
    const p = sut.create({
      code: 'WelcomeEmail',
      subjectAr: 's-ar', subjectEn: 's-en',
      bodyAr: 'b-ar', bodyEn: 'b-en',
      channel: 'Email', variableSchemaJson: '{}',
    });
    const req = http.expectOne('/api/admin/notification-templates');
    expect(req.request.method).toBe('POST');
    req.flush({});
    await p;
  });

  it('update PUTs body', async () => {
    const p = sut.update('t1', {
      subjectAr: 's-ar', subjectEn: 's-en',
      bodyAr: 'b-ar', bodyEn: 'b-en',
      isActive: true,
    });
    const req = http.expectOne('/api/admin/notification-templates/t1');
    expect(req.request.method).toBe('PUT');
    req.flush({});
    await p;
  });
});
