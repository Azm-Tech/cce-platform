import {
  EnvironmentProviders,
  inject,
  makeEnvironmentProviders,
  provideAppInitializer,
} from '@angular/core';
import { REALTIME_CONFIG, RealtimeConfig } from './realtime.config';
import { RealtimeHubService } from './realtime-hub.service';

/**
 * Registers the shared notifications hub for an app.
 *
 * Takes a **config factory** (not a plain object) so it runs in an injection
 * context and can `inject(EnvService)` / `inject(AuthService)`:
 *
 * ```ts
 * provideRealtime(() => {
 *   const env = inject(EnvService);
 *   const auth = inject(AuthService);
 *   return {
 *     hubUrlFactory: () => `${env.env.apiBaseUrl}/hubs/notifications`,
 *     accessToken: auth.accessToken,
 *     isAuthenticated: auth.isAuthenticated,
 *   };
 * }),
 * ```
 */
export function provideRealtime(configFactory: () => RealtimeConfig): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: REALTIME_CONFIG, useFactory: configFactory },
    // Eagerly instantiate the service so its connect/disconnect effect is live.
    provideAppInitializer(() => {
      inject(RealtimeHubService);
    }),
  ]);
}
