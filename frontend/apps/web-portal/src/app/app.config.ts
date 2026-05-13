import { provideHttpClient, withFetch, withInterceptors, HttpClient } from '@angular/common/http';
import { ApplicationConfig, provideAppInitializer, provideZoneChangeDetection, inject } from '@angular/core';
import { AuthService } from './core/auth/auth.service';
import { bffCredentialsInterceptor } from './core/http/bff-credentials.interceptor';
import { correlationIdInterceptor } from './core/http/correlation-id.interceptor';
import { serverErrorInterceptor } from './core/http/server-error.interceptor';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter } from '@angular/router';
import { TranslateLoader, TranslateModule, TranslateService } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { firstValueFrom } from 'rxjs';
import { appRoutes } from './app.routes';
import { EnvService } from './core/env.service';
import { ngxTranslateHttpLoaderFactory } from './core/translate-loader.factory';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(appRoutes),
    provideHttpClient(
      withFetch(),
      withInterceptors([correlationIdInterceptor, bffCredentialsInterceptor, serverErrorInterceptor]),
    ),
    provideAnimationsAsync(),
    ...(TranslateModule.forRoot({
      loader: {
        provide: TranslateLoader,
        useFactory: ngxTranslateHttpLoaderFactory,
        deps: [HttpClient],
      },
      defaultLanguage: 'ar',
    }).providers ?? []),
    provideAppInitializer(async () => {
      const env = inject(EnvService);
      const translate = inject(TranslateService);
      const locale = inject(LocaleService);
      const auth = inject(AuthService);
      await env.load();
      translate.setDefaultLang('ar');
      await firstValueFrom(translate.use(locale.locale()));
      // Bootstrap auth state from /api/me. Without this the SPA never
      // discovers an existing BFF cookie session — so after a successful
      // login redirect the header keeps showing the "Sign in" button as
      // if nothing happened. Tolerates 401 silently (anonymous user).
      await auth.refresh();
    }),
  ],
};
