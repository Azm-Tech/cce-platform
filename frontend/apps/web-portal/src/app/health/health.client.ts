import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { EnvService } from '../core/env.service';

export interface HealthResponse {
  status: string;
  version: string;
  locale: string;
  utcNow: string;
}

@Injectable({ providedIn: 'root' })
export class HealthClient {
  private readonly http = inject(HttpClient);
  private readonly env = inject(EnvService);

  fetch(): Observable<HealthResponse> {
    return this.http.get<HealthResponse>(`${this.env.env.apiBaseUrl}/health`);
  }
}
