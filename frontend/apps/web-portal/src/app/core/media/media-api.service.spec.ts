import { HttpEventType, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { MediaApiService } from './media-api.service';

describe('MediaApiService', () => {
  let sut: MediaApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    sut = TestBed.inject(MediaApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('uploadFile POSTs to /api/media', async () => {
    const file = new File(['foo'], 'foo.txt', { type: 'text/plain' });
    const promise = sut.uploadFile(file);
    const req = http.expectOne('/api/media');
    expect(req.request.method).toBe('POST');
    expect(req.request.body.get('file')).toBe(file);
    req.flush({ data: { id: 'm1', url: '/media/1' } });
    const res = await promise;
    expect(res.ok).toBe(true);
    if (res.ok) expect(res.value.id).toBe('m1');
  });

  it('uploadFileWithProgress tracks progress and returns media asset', (done) => {
    const file = new File(['foo'], 'foo.txt', { type: 'text/plain' });
    const updates: any[] = [];

    sut.uploadFileWithProgress(file).subscribe({
      next: (update) => {
        updates.push(update);
      },
      complete: () => {
        expect(updates).toEqual([
          { progress: 50 },
          { progress: 100, asset: { id: 'm1', url: '/media/1' } },
        ]);
        done();
      },
    });

    const req = http.expectOne('/api/media');
    expect(req.request.method).toBe('POST');

    // Simulate progress event
    req.event({
      type: HttpEventType.UploadProgress,
      loaded: 50,
      total: 100,
    });

    // Simulate final response
    req.flush({ data: { id: 'm1', url: '/media/1' } });
  });

  it('uploadFileWithProgress handles errors', (done) => {
    const file = new File(['foo'], 'foo.txt', { type: 'text/plain' });

    sut.uploadFileWithProgress(file).subscribe({
      next: (update) => {
        expect(update.error).toBeDefined();
        expect(update.error?.kind).toBe('server');
        done();
      },
    });

    const req = http.expectOne('/api/media');
    req.flush('', { status: 500, statusText: 'Server Error' });
  });
});
