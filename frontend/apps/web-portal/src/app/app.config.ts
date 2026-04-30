import { provideHttpClient, withFetch, withInterceptors, HttpClient } from '@angular/common/http';
import { ApplicationConfig, provideAppInitializer, provideZoneChangeDetection, inject } from '@angular/core';
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
      await env.load();
      translate.setDefaultLang('ar');
      await firstValueFrom(translate.use(locale.locale()));
    }),
  ],
};
