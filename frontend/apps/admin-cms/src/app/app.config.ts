import { provideHttpClient, withFetch, withInterceptors, HttpClient } from '@angular/common/http';
import { authInterceptor } from './core/http/auth.interceptor';
import { serverErrorInterceptor } from './core/http/server-error.interceptor';
import { correlationIdInterceptor } from './core/http/correlation-id.interceptor';
import { ApplicationConfig, provideAppInitializer, provideZoneChangeDetection, inject, isDevMode } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter } from '@angular/router';
import { provideTransloco, TranslocoService } from '@jsverse/transloco';
import { provideAuth } from 'angular-auth-oidc-client';
import { LocaleService } from '@frontend/i18n';
import { buildCceOidcConfig } from '@frontend/auth';
import { appRoutes } from './app.routes';
import { AuthService } from './core/auth/auth.service';
import { EnvService } from './core/env.service';
import { TranslocoHttpLoader } from '@frontend/i18n';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(appRoutes),
    provideHttpClient(
      withFetch(),
      withInterceptors([correlationIdInterceptor, authInterceptor, serverErrorInterceptor]),
    ),
    provideAnimationsAsync(),
    provideTransloco({
      config: {
        availableLangs: ['en', 'ar'],
        defaultLang: 'ar',
        prodMode: !isDevMode(),
      },
      loader: TranslocoHttpLoader
    }),
    provideAuth({
      config: buildCceOidcConfig({
        authority: 'http://localhost:8080/realms/cce-internal',
        clientId: 'cce-admin-cms',
        redirectUri:
          typeof window !== 'undefined'
            ? `${window.location.origin}/auth/callback`
            : 'http://localhost:4201/auth/callback',
        postLogoutRedirectUri:
          typeof window !== 'undefined' ? window.location.origin : 'http://localhost:4201',
      }),
    }),
    provideAppInitializer(async () => {
      const env = inject(EnvService);
      const translate = inject(TranslocoService);
      const locale = inject(LocaleService);
      const auth = inject(AuthService);
      await env.load();
      translate.setActiveLang(locale.locale());
      await auth.refresh();
    }),
  ],
};
