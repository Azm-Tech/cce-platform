import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { LocaleService } from './locale.service';

/**
 * Stamps every outgoing request with Accept-Language so the backend can
 * return error messages and content in the user's chosen locale.
 * Accept-Language is a CORS-safelisted header — no preflight impact.
 */
export const localeInterceptor: HttpInterceptorFn = (req, next) => {
  const locale = inject(LocaleService).locale();
  return next(req.clone({ setHeaders: { 'Accept-Language': locale } }));
};
