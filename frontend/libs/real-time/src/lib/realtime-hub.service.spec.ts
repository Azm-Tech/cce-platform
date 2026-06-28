import { ApplicationRef, signal, type WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';

// ── Mock @microsoft/signalr ──────────────────────────────────────────────────
// The factory owns a single fake connection; tests drive it via `__conn`.
jest.mock('@microsoft/signalr', () => {
  const handlers = new Map<string, (raw: unknown) => void>();
  const conn: Record<string, unknown> = {
    state: 'Disconnected',
    _handlers: handlers,
    _reconnected: undefined as undefined | (() => void),
    _emit(name: string, raw: unknown) {
      handlers.get(name)?.(raw);
    },
    on: jest.fn((name: string, cb: (raw: unknown) => void) => handlers.set(name, cb)),
    start: jest.fn(async () => {
      conn['state'] = 'Connected';
    }),
    stop: jest.fn(async () => {
      conn['state'] = 'Disconnected';
    }),
    invoke: jest.fn(async () => undefined),
    onreconnecting: jest.fn(),
    onreconnected: jest.fn((cb: () => void) => {
      conn['_reconnected'] = cb;
    }),
    onclose: jest.fn(),
  };
  return {
    __conn: conn,
    HubConnectionBuilder: jest.fn(() => ({
      withUrl: jest.fn().mockReturnThis(),
      withAutomaticReconnect: jest.fn().mockReturnThis(),
      configureLogging: jest.fn().mockReturnThis(),
      build: () => conn,
    })),
    HubConnectionState: { Connected: 'Connected', Disconnected: 'Disconnected' },
    LogLevel: { Warning: 3 },
  };
});

import * as signalrMock from '@microsoft/signalr';
import { RealtimeHubService } from './realtime-hub.service';
import { REALTIME_CONFIG, type RealtimeConfig } from './realtime.config';
import { RealtimeEvent, type NewReplyPayload } from './realtime.types';

interface FakeConn {
  state: string;
  invoke: jest.Mock;
  start: jest.Mock;
  stop: jest.Mock;
  _reconnected?: () => void;
  _emit: (name: string, raw: unknown) => void;
  _handlers: Map<string, unknown>;
}

const conn = (signalrMock as unknown as { __conn: FakeConn }).__conn;

/** Flush signal effects, then let the async lifecycle promise-chain settle. */
async function settle(): Promise<void> {
  TestBed.inject(ApplicationRef).tick();
  for (let i = 0; i < 6; i++) await Promise.resolve();
}

describe('RealtimeHubService', () => {
  let isAuth: WritableSignal<boolean>;
  let token: WritableSignal<string | null>;

  beforeEach(() => {
    jest.clearAllMocks();
    conn.state = 'Disconnected';
    conn._handlers.clear();
    conn._reconnected = undefined;

    isAuth = signal(false);
    token = signal<string | null>('tok-1');

    const config: RealtimeConfig = {
      hubUrlFactory: () => '/hubs/notifications',
      accessToken: token,
      isAuthenticated: isAuth,
      debug: false,
    };

    TestBed.configureTestingModule({
      providers: [{ provide: REALTIME_CONFIG, useValue: config }],
    });
  });

  it('connects when authenticated and disconnects on logout', async () => {
    const hub = TestBed.inject(RealtimeHubService);

    isAuth.set(true);
    await settle();
    expect(conn.start).toHaveBeenCalledTimes(1);
    expect(hub.isConnected()).toBe(true);

    isAuth.set(false);
    await settle();
    expect(conn.stop).toHaveBeenCalled();
    expect(hub.isConnected()).toBe(false);
  });

  it('unwraps the RealtimeEnvelope, dedups by eventId, and tracks the cursor', async () => {
    const hub = TestBed.inject(RealtimeHubService);
    const received: NewReplyPayload[] = [];
    hub.on<NewReplyPayload>(RealtimeEvent.NewReply).subscribe((p) => received.push(p));

    isAuth.set(true);
    await settle();

    const envelope = {
      eventId: 'evt-1',
      occurredOn: '2026-01-02T03:04:05Z',
      payload: { postId: 'p1', replyId: 'r1', parentReplyId: null, depth: 0 },
    };
    conn._emit('NewReply', envelope);
    conn._emit('NewReply', envelope); // duplicate eventId — must be ignored

    expect(received).toHaveLength(1);
    expect(received[0].postId).toBe('p1');
    expect(hub.lastEventTime).toBe('2026-01-02T03:04:05Z');
  });

  it('invokes Subscribe and replays subscriptions after reconnect', async () => {
    const hub = TestBed.inject(RealtimeHubService);

    isAuth.set(true);
    await settle();

    hub.subscribePost('post-1');
    await settle();
    expect(conn.invoke).toHaveBeenCalledWith('Subscribe', 'post-1');

    conn.invoke.mockClear();
    conn._reconnected?.(); // simulate automatic reconnect
    await settle();
    expect(conn.invoke).toHaveBeenCalledWith('Subscribe', 'post-1');
  });

  it('stays idle when no REALTIME_CONFIG is provided', () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({ providers: [] });
    const hub = TestBed.inject(RealtimeHubService);
    expect(hub.isConnected()).toBe(false);
    expect(conn.start).not.toHaveBeenCalled();
  });
});
