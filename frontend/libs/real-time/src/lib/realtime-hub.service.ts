import { Injectable, computed, effect, inject, signal } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';
import { REALTIME_CONFIG } from './realtime.config';
import { normalizePayload } from './realtime-normalize';
import type { RealtimeConnectionState, RealtimeEnvelope, RealtimeEventName } from './realtime.types';

type GroupKind = 'post' | 'topic' | 'community';

/** Hub method that joins each group kind. */
const SUBSCRIBE_METHOD: Record<GroupKind, string> = {
  post: 'Subscribe',
  topic: 'SubscribeTopic',
  community: 'SubscribeCommunity',
};

/**
 * Single SignalR connection to the notifications hub, shared per app.
 *
 * - Connects when the user is authenticated, disconnects on logout.
 * - Reconnects automatically on network drops and replays entity subscriptions.
 * - Re-handshakes when the access token rotates (SignalR only reads
 *   `accessTokenFactory` at connect time, so a rotated token would otherwise
 *   leave the server-side `Context.User` on stale claims).
 */
@Injectable({ providedIn: 'root' })
export class RealtimeHubService {
  // Optional so the service is safe to instantiate without `provideRealtime`
  // (e.g. in component unit tests) — it just stays idle.
  private readonly config = inject(REALTIME_CONFIG, { optional: true });

  private connection: HubConnection | null = null;
  /** Active entity subscriptions as `"<kind>:<id>"`, replayed after (re)connect. */
  private readonly activeGroups = new Set<string>();
  /** Lazily-created multicast stream per event name; survives reconnects. */
  private readonly streams = new Map<string, Subject<unknown>>();
  /** Token captured at the last successful connect; used to detect rotation. */
  private connectedToken: string | null = null;
  /** Recent envelope eventIds (ring buffer) — dedup duplicates after reconnects. */
  private readonly seenEventIds: string[] = [];
  private readonly seenEventIdSet = new Set<string>();
  /** occurredOn of the last processed envelope — reconnect catch-up cursor. */
  private lastOccurredOn: string | null = null;
  /** Serializes connect/disconnect/reconnect so they never overlap. */
  private lifecycle: Promise<void> = Promise.resolve();

  private readonly _state = signal<RealtimeConnectionState>('disconnected');
  readonly connectionState = this._state.asReadonly();
  readonly isConnected = computed(() => this._state() === 'connected');

  /** ISO timestamp of the last processed event — use as the `since` cursor for
   *  reconnect catch-up (`GET …/activity?since=`). */
  get lastEventTime(): string | null {
    return this.lastOccurredOn;
  }

  private setState(state: RealtimeConnectionState): void {
    this._state.set(state);
    if (this.config?.debug) console.debug('[realtime] state →', state);
  }

  constructor() {
    const cfg = this.config;
    if (!cfg) return; // no app wiring → idle service

    // Connect on login, tear down on logout.
    effect(() => {
      if (cfg.isAuthenticated()) {
        this.enqueue(() => this.openConnection());
      } else {
        this.enqueue(async () => {
          this.activeGroups.clear();
          await this.closeConnection();
        });
      }
    });

    // Reconnect when the token rotates while connected, so the fresh JWT
    // re-handshakes the socket.
    effect(() => {
      const token = cfg.accessToken();
      if (
        this.connection &&
        this._state() === 'connected' &&
        token &&
        this.connectedToken &&
        token !== this.connectedToken
      ) {
        this.enqueue(() => this.reconnect());
      }
    });
  }

  // ── Public subscription API ────────────────────────────────────────────────
  subscribePost(postId: string): void { this.joinGroup('post', postId); }
  unsubscribePost(postId: string): void { this.leaveGroup('post', postId, 'Unsubscribe'); }
  subscribeTopic(topicId: string): void { this.joinGroup('topic', topicId); }
  unsubscribeTopic(topicId: string): void { this.leaveGroup('topic', topicId, 'UnsubscribeTopic'); }
  subscribeCommunity(communityId: string): void { this.joinGroup('community', communityId); }
  unsubscribeCommunity(communityId: string): void {
    this.leaveGroup('community', communityId, 'UnsubscribeCommunity');
  }

  startTyping(postId: string): void { void this.invoke('StartTyping', postId); }
  stopTyping(postId: string): void { void this.invoke('StopTyping', postId); }

  /**
   * Typed, case-normalized stream for a server→client event. The same stream is
   * shared by all subscribers and persists across reconnects, so subscribe once
   * (e.g. with `takeUntilDestroyed`) in a component.
   */
  on<T>(event: RealtimeEventName): Observable<T> {
    return this.streamFor(event) as unknown as Observable<T>;
  }

  // ── Lifecycle ────────────────────────────────────────────────────────────────
  private enqueue(op: () => Promise<void>): void {
    this.lifecycle = this.lifecycle.then(op).catch(() => undefined);
  }

  private async openConnection(): Promise<void> {
    const cfg = this.config;
    if (!cfg || this.connection) return;
    const token = cfg.accessToken();
    const connection = new HubConnectionBuilder()
      .withUrl(cfg.hubUrlFactory(), {
        accessTokenFactory: () => cfg.accessToken() ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.onreconnecting(() => this.setState('reconnecting'));
    connection.onreconnected(() => {
      this.setState('connected');
      void this.replayGroups();
    });
    connection.onclose(() => this.setState('disconnected'));

    for (const event of this.streams.keys()) {
      this.attachHandler(connection, event);
    }

    this.connection = connection;
    this.setState('connecting');
    try {
      await connection.start();
      this.connectedToken = token;
      this.setState('connected');
      await this.replayGroups();
    } catch {
      this.connection = null;
      this.connectedToken = null;
      this.setState('disconnected');
    }
  }

  private async closeConnection(): Promise<void> {
    const connection = this.connection;
    this.connection = null;
    this.connectedToken = null;
    this.setState('disconnected');
    if (connection) {
      try { await connection.stop(); } catch { /* already stopped */ }
    }
  }

  private async reconnect(): Promise<void> {
    await this.closeConnection();
    await this.openConnection();
  }

  // ── Groups & events ──────────────────────────────────────────────────────────
  private joinGroup(kind: GroupKind, id: string): void {
    this.activeGroups.add(`${kind}:${id}`);
    if (this.isConnected()) void this.invoke(SUBSCRIBE_METHOD[kind], id);
  }

  private leaveGroup(kind: GroupKind, id: string, method: string): void {
    this.activeGroups.delete(`${kind}:${id}`);
    if (this.isConnected()) void this.invoke(method, id);
  }

  private async replayGroups(): Promise<void> {
    for (const group of this.activeGroups) {
      const idx = group.indexOf(':');
      const kind = group.slice(0, idx) as GroupKind;
      const id = group.slice(idx + 1);
      await this.invoke(SUBSCRIBE_METHOD[kind], id);
    }
  }

  private streamFor(event: string): Subject<unknown> {
    let stream = this.streams.get(event);
    if (!stream) {
      stream = new Subject<unknown>();
      this.streams.set(event, stream);
      if (this.connection) this.attachHandler(this.connection, event);
    }
    return stream;
  }

  private attachHandler(connection: HubConnection, event: string): void {
    connection.on(event, (raw: unknown) => {
      // Pushes are wrapped in a RealtimeEnvelope { eventId, occurredOn, payload }.
      // Unwrap it, dedup on eventId (reconnects can replay), and track the cursor.
      // Tolerate an un-enveloped payload too (defensive during rollout).
      let payload: unknown = raw;
      if (raw && typeof raw === 'object' && 'payload' in (raw as Record<string, unknown>)) {
        const env = raw as Partial<RealtimeEnvelope>;
        if (typeof env.eventId === 'string' && !this.rememberEvent(env.eventId)) return;
        if (typeof env.occurredOn === 'string') this.lastOccurredOn = env.occurredOn;
        payload = env.payload;
      }
      const normalized = normalizePayload(payload);
      if (this.config?.debug) console.debug(`[realtime] ◀ ${event}`, normalized);
      this.streams.get(event)?.next(normalized);
    });
  }

  /** Records an eventId; returns false if it was already seen (duplicate). */
  private rememberEvent(id: string): boolean {
    if (this.seenEventIdSet.has(id)) return false;
    this.seenEventIdSet.add(id);
    this.seenEventIds.push(id);
    if (this.seenEventIds.length > 200) {
      const oldest = this.seenEventIds.shift();
      if (oldest) this.seenEventIdSet.delete(oldest);
    }
    return true;
  }

  private async invoke(method: string, ...args: unknown[]): Promise<void> {
    if (this.connection?.state !== HubConnectionState.Connected) return;
    try {
      await this.connection.invoke(method, ...args);
    } catch {
      // Transient invoke failures (e.g. auth race after token TTL) must not crash callers.
    }
  }
}
