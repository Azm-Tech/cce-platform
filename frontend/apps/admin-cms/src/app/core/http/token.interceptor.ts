import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../auth/auth.service';
import { isInternalUrl } from './is-internal-url';

const SKIP_PATHS = ['/api/auth/login', '/api/auth/refresh', '/api/auth/logout'];

export const tokenInterceptor: HttpInterceptorFn = (req, next) => {
  if (!isInternalUrl(req.url)) return next(req);
  if (SKIP_PATHS.some((p) => req.url.includes(p))) return next(req);
  const token = inject(AuthService).accessToken();
  if (!token) return next(req);
  return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};
