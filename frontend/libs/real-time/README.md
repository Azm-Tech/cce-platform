# real-time

`@frontend/real-time` — shared SignalR notifications hub client for the CCE Platform
(web-portal and admin-cms).

Owns a single `HubConnection` per app, started after login and torn down on logout. It
re-connects automatically on network drops, replays entity subscriptions after a reconnect,
and re-handshakes when the access token rotates (SignalR only reads `accessTokenFactory` at
connect time).

## Usage

Wire it once in each app's `app.config.ts`:

```ts
provideRealtime(() => {
  const auth = inject(AuthService);
  return {
    // Relative, same-origin path — routed to the backend by the proxy, like /api/*.
    hubUrlFactory: () => '/hubs/notifications',
    accessToken: auth.accessToken,
    isAuthenticated: auth.isAuthenticated,
    debug: isDevMode(),
  };
}),
```

Then inject `RealtimeHubService` in features:

```ts
private readonly hub = inject(RealtimeHubService);

ngOnInit() {
  this.hub.subscribePost(this.postId);
  this.hub.on<NewReplyPayload>(RealtimeEvent.NewReply)
    .pipe(takeUntilDestroyed(this.destroyRef))
    .subscribe(p => { /* refetch / append */ });
}
ngOnDestroy() {
  this.hub.unsubscribePost(this.postId);
}
```

See `cce-platform-docs/technical/REALTIME_NOTIFICATIONS_PLAN.md` for the full event contract.

## Running unit tests

Run `nx test real-time` to execute the unit tests via Jest.
