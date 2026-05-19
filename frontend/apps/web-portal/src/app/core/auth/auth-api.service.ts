import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface LoginRequest {
  emailAddress: string;
  password: string;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  emailAddress: string;
  jobTitle: string;
  organizationName: string;
  phoneNumber: string;
  password: string;
  confirmPassword: string;
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
  roles: string[];
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

  register(req: RegisterRequest): Observable<void> {
    return this.http.post<void>('/api/auth/register', req);
  }

  forgotPassword(emailAddress: string): Observable<void> {
    return this.http.post<void>('/api/auth/forgot-password', { emailAddress });
  }

  resetPassword(req: {
    emailAddress: string;
    token: string;
    newPassword: string;
    confirmPassword: string;
  }): Observable<void> {
    return this.http.post<void>('/api/auth/reset-password', req);
  }

  logout(refreshToken: string): Observable<void> {
    return this.http.post<void>('/api/auth/logout', { refreshToken });
  }

  refresh(refreshToken: string): Observable<TokenPair> {
    return this.http
      .post<ApiEnvelope<TokenPair>>('/api/auth/refresh', { refreshToken })
      .pipe(map((res) => res.data));
  }
}
