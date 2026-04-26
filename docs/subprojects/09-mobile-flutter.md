# Sub-project 09: Mobile (Flutter)

## Goal

Ship a Flutter-based WebView shell wrapping the External Web Portal for **iOS**, **Android**, and **Huawei** distribution. Native shell concerns only: app store delivery, push notifications, deep links, biometric session unlock, splash, and offline messaging. The web portal does the heavy UI lifting — the mobile shell does not duplicate feature code.

## BRD references

- HLD §3.2.2 — Mobile architecture (WebView shell).
- §4.1.1–4.1.18 — Public requirements (delivered by the embedded portal).

## Dependencies

- Sub-project 6 (Web Portal) — provides the URL the WebView loads.

## Rough estimate

T-shirt size: **M**.

## DoD skeleton

- [ ] Flutter project layout supporting iOS, Android, Huawei (HMS).
- [ ] WebView loads the portal with appropriate UA + cookie storage; biometric session unlock.
- [ ] Push notification integration (APNs + FCM + HMS Push).
- [ ] Deep links from notifications navigate to specific portal routes inside the WebView.
- [ ] App store metadata (icons, splash, screenshots) for iOS, Android, Huawei.
- [ ] Offline detection: graceful "no connection" screen instead of WebView error.
- [ ] Crash reporting (Sentry Flutter SDK with the same DSN-empty-no-op pattern as web).
- [ ] Build pipelines for each store (Fastlane or equivalent).

Refined at this sub-project's own brainstorm cycle.

## Related

- ADRs: [0010](../adr/0010-sentry-error-tracking.md).
