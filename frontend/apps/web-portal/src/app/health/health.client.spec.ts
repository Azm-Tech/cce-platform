import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import type { CceEnv } from '@frontend/contracts';
import { EnvService } from '../core/env.service';
import { HealthClient } from './health.client';

describe('HealthClient', () => {
  let client: HealthClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: EnvService,
          useValue: {
            env: {
              environment: 'test', apiBaseUrl: 'http://api.test',
              oidcAuthority: '', oidcClientId: '', sentryDsn: '',
            } satisfies CceEnv,
          },
        },
        HealthClient,
      ],
    });
    client = TestBed.inject(HealthClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('GETs apiBaseUrl + /health', async () => {
    const promise = firstValueFrom(client.fetch());
    const req = httpMock.expectOne('http://api.test/health');
    expect(req.request.method).toBe('GET');
    req.flush({ status: 'ok', version: '0.1.0', locale: 'ar', utcNow: '2026-04-25T00:00:00Z' });

    const result = await promise;
    expect(result.status).toBe('ok');
  });
});
