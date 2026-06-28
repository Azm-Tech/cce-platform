import { InjectionToken, Signal } from '@angular/core';

/**
 * App-supplied wiring for {@link RealtimeHubService}. Provided via
 * {@link provideRealtime} so the shared lib stays decoupled from each app's
 * `AuthService` / `EnvService`.
 */
export interface RealtimeConfig {
  /**
   * Builds the hub URL (called lazily at connect time). Use a relative,
   * same-origin path — `'/hubs/notifications'` — so it's routed to the backend
   * by the dev proxy / reverse proxy, exactly like the `/api/*` calls.
   */
  hubUrlFactory: () => string;

  /**
   * Current JWT access token (null when unauthenticated). Feeds SignalR's
   * `accessTokenFactory` and drives a reconnect when the token rotates.
   */
  accessToken: Signal<string | null>;

  /** Auth state — the hub connects when this is true and disconnects when false. */
  isAuthenticated: Signal<boolean>;

  /**
   * When true, logs connection-state transitions and every received event
   * (normalized + raw payload) to the console. Wire to `isDevMode()`.
   */
  debug?: boolean;
}

export const REALTIME_CONFIG = new InjectionToken<RealtimeConfig>('REALTIME_CONFIG');
