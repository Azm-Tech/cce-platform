import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { CceAdminRole, CcePortalRole } from '@frontend/contracts';

export interface LoginRequest {
  emailAddress: string;
  password: string;
}

interface ApiEnvelope<T> {
  success: boolean;
  code: string;
  message: string;
  data: T;
  errors: string[];
  traceId: string;
  timestamp: string;
}

export interface AuthUser {
  id: string;
  emailAddress: string;
  firstName: string;
  lastName: string;
  roles: (CceAdminRole | CcePortalRole)[];
}

export interface TokenPair {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  tokenType: string;
  user: AuthUser;
}

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly http = inject(HttpClient);

  login(req: LoginRequest): Observable<TokenPair> {
    return this.http
      .post<ApiEnvelope<TokenPair>>('/api/auth/login', req)
      .pipe(map((res) => res.data));
  }

  logout(refreshToken: string): Observable<void> {
    return this.http.post<void>('/api/auth/logout', { refreshToken });
  }

  forgotPassword(emailAddress: string, context?: HttpContext): Observable<void> {
    return this.http.post<void>('/api/auth/forgot-password', { emailAddress }, { context });
  }

  resetPassword(req: {
    emailAddress: string;
    token: string;
    newPassword: string;
    confirmPassword: string;
  }): Observable<void> {
    return this.http.post<void>('/api/auth/reset-password', req);
  }

  refresh(refreshToken: string): Observable<TokenPair> {
    return this.http
      .post<ApiEnvelope<TokenPair>>('/api/auth/refresh', { refreshToken })
      .pipe(map((res) => res.data));
  }
}
