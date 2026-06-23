import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideAppInitializer, provideZoneChangeDetection, inject, isDevMode } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideTransloco, TranslocoService } from '@jsverse/transloco';
import { MAT_FORM_FIELD_DEFAULT_OPTIONS } from '@angular/material/form-field';
import { apiEnvelopeInterceptor, serverErrorInterceptor, provideCceIcons } from '@frontend/ui-kit';
import { localeInterceptor, LocaleService, TranslocoHttpLoader } from '@frontend/i18n';
import { provideRealtime } from '@frontend/real-time';
import { tokenInterceptor } from './core/http/token.interceptor';
import { authInterceptor } from './core/http/auth.interceptor';
import { correlationIdInterceptor } from './core/http/correlation-id.interceptor';
import { appRoutes } from './app.routes';
import { AuthService } from './core/auth/auth.service';
import { EnvService } from './core/env.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    { provide: MAT_FORM_FIELD_DEFAULT_OPTIONS, useValue: { appearance: 'outline' } },
    provideCceIcons(),
    provideRouter(appRoutes),
    provideHttpClient(
      withFetch(),
      withInterceptors([localeInterceptor, correlationIdInterceptor, tokenInterceptor, serverErrorInterceptor, authInterceptor, apiEnvelopeInterceptor]),
    ),
    provideTransloco({
      config: {
        availableLangs: ['en', 'ar'],
        defaultLang: 'ar',
        reRenderOnLangChange: true,
        prodMode: !isDevMode(),
      },
      loader: TranslocoHttpLoader,
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
    provideRealtime(() => {
      const auth = inject(AuthService);
      return {
        // Relative path, same-origin — the proxy routes /hubs to the External API
        // (where the hub lives), even though /api goes to the Internal API.
        hubUrlFactory: () => '/hubs/notifications',
        accessToken: auth.accessToken,
        isAuthenticated: auth.isAuthenticated,
        debug: isDevMode(),
      };
    }),
  ],
};
