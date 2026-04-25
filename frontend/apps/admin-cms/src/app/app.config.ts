import { provideHttpClient, withInterceptorsFromDi, HttpClient } from '@angular/common/http';
import { ApplicationConfig, provideAppInitializer, provideZoneChangeDetection, inject } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter } from '@angular/router';
import { TranslateLoader, TranslateModule, TranslateService } from '@ngx-translate/core';
import { provideAuth } from 'angular-auth-oidc-client';
import { firstValueFrom } from 'rxjs';
import { LocaleService } from '@frontend/i18n';
import { buildCceOidcConfig } from '@frontend/auth';
import { appRoutes } from './app.routes';
import { EnvService } from './core/env.service';
import { ngxTranslateHttpLoaderFactory } from './core/translate-loader.factory';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(appRoutes),
    provideHttpClient(withInterceptorsFromDi()),
    provideAnimationsAsync(),
    ...(TranslateModule.forRoot({
      loader: {
        provide: TranslateLoader,
        useFactory: ngxTranslateHttpLoaderFactory,
        deps: [HttpClient],
      },
      defaultLanguage: 'ar',
    }).providers ?? []),
    // OIDC config is built dynamically AFTER env.json loads, so provideAuth uses a placeholder
    // here and we re-configure it inside provideAppInitializer once env is available.
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
      await env.load();
      const translate = inject(TranslateService);
      const locale = inject(LocaleService);
      translate.setDefaultLang('ar');
      await firstValueFrom(translate.use(locale.locale()));
    }),
  ],
};
