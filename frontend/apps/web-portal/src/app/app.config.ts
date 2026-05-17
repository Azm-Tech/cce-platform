import { provideHttpClient, withFetch, withInterceptors, HttpClient } from '@angular/common/http';
import { ApplicationConfig, provideAppInitializer, provideZoneChangeDetection, inject, isDevMode } from '@angular/core';
import { AuthService } from './core/auth/auth.service';
import { bffCredentialsInterceptor } from './core/http/bff-credentials.interceptor';
import { correlationIdInterceptor } from './core/http/correlation-id.interceptor';
import { serverErrorInterceptor } from '@frontend/ui-kit';
import { localeInterceptor } from '@frontend/i18n';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter } from '@angular/router';
import { provideTransloco, TranslocoService } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { appRoutes } from './app.routes';
import { EnvService } from './core/env.service';
import { TranslocoHttpLoader } from '@frontend/i18n';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(appRoutes),
    provideHttpClient(
      withFetch(),
      withInterceptors([localeInterceptor, correlationIdInterceptor, bffCredentialsInterceptor, serverErrorInterceptor]),
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
