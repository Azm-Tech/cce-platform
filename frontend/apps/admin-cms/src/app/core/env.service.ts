import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import type { CceEnv } from '@frontend/contracts';
import { firstValueFrom } from 'rxjs';

/**
 * Loads /assets/env.json once at app bootstrap so runtime config is available
 * before any other service needs it. Apps call <code>load()</code> from an
 * APP_INITIALIZER provider; consumers read <code>env</code> synchronously
 * thereafter.
 */
@Injectable({ providedIn: 'root' })
export class EnvService {
  private readonly http = inject(HttpClient);
  private cached: CceEnv | null = null;

  async load(): Promise<void> {
    try {
      this.cached = await firstValueFrom(this.http.get<CceEnv>('/assets/env.json'));
    } catch (cause) {
      const message = cause instanceof Error ? cause.message : String(cause);
      throw new Error(`Failed to load /assets/env.json: ${message}`);
    }
  }

  get env(): CceEnv {
    if (!this.cached) {
      throw new Error('EnvService.env accessed before load() resolved.');
    }
    return this.cached;
  }
}
