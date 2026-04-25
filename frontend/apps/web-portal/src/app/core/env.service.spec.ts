import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import type { CceEnv } from '@frontend/contracts';
import { EnvService } from './env.service';

describe('EnvService', () => {
  let service: EnvService;
  let httpMock: HttpTestingController;

  const fixture: CceEnv = {
    environment: 'test',
    apiBaseUrl: 'http://api.test',
    oidcAuthority: 'http://oidc.test',
    oidcClientId: 'test-client',
    sentryDsn: '',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), EnvService],
    });
    service = TestBed.inject(EnvService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('throws when accessed before load', () => {
    expect(() => service.env).toThrow();
  });

  it('loads env.json once and exposes the typed config', async () => {
    const promise = service.load();
    httpMock.expectOne('/assets/env.json').flush(fixture);
    await promise;

    expect(service.env).toEqual(fixture);
  });

  it('rejects on HTTP error', async () => {
    const promise = service.load();
    httpMock.expectOne('/assets/env.json').error(new ProgressEvent('error'), { status: 500, statusText: 'fail' });

    await expect(promise).rejects.toThrow();
  });
});
