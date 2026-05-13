# Phase 05 — Multi-map tabs

> Parent: [`../2026-05-01-sub-7.md`](../2026-05-01-sub-7.md) · Spec: [`../../specs/2026-05-01-sub-7-design.md`](../../specs/2026-05-01-sub-7-design.md) §8 (UX: tabs as the multi-map pattern), §9 (open second / close tab user flows)

**Phase goal:** Land a `TabsBar` above the search row so the user can hold multiple maps open at once. Click a tab to switch active. Click the `×` to close. The active tab is always the route's `:id`. Other open tabs ride the `?open=a,b,c` URL param. Closing the last tab navigates back to `/knowledge-maps` (the list page). After Phase 05, the multi-map workflow is complete.

**Tasks:** 4
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 04 closed (`f6d115c`).
- web-portal: 336/336 Jest tests passing; lint + build clean.
- Store already has `openTab(id)` / `closeTab(id)` / `setActive(id)` / `openTabs` computed (Phase 1.1).

---

## Task 5.1: `TabsBarComponent`

**Files (new):**
- `viewer/tabs-bar.component.{ts,html,scss,spec.ts}`

Signal inputs:
- `tabs: input.required<ViewerTab[]>()`
- `activeId: input<string | null>(null)`
- `locale: input<'ar' | 'en'>('en')`

Outputs:
- `tabSelected = output<string>()` — emitted when user clicks a non-active tab
- `tabClosed = output<string>()` — emitted when user clicks the × on a tab

Renders one button per tab in a horizontal scroll-x strip. Each tab shows the localized map name (`metadata.nameAr` / `metadata.nameEn`) and a close icon button. The active tab gets a brand-blue underline; inactive tabs are subdued. Tab strip scrolls horizontally on overflow (mobile-friendly).

When `tabs` is empty, the component renders nothing — `@if (tabs().length > 0)` wraps the strip.

Tests (~5):
1. Renders one button per tab.
2. Active tab has the active class / underline.
3. Locale toggle switches the visible label between `nameAr` and `nameEn`.
4. Clicking a non-active tab emits `(tabSelected)` with that tab's id.
5. Clicking the × on a tab emits `(tabClosed)` with that id; the click does NOT also fire `(tabSelected)` (stop-propagation).

Commit: `feat(web-portal): TabsBarComponent (Phase 5.1)`

---

## Task 5.2: URL `?open=` sync + open-from-URL on init

**Files (modify):**
- `map-viewer.page.ts` — extend the existing URL-sync `effect()` to also reflect tab state (the array of *other* open tabs, excluding the active one) into `?open=`. Hydrate `?open=` on init by calling `store.openTab(id)` for each id (with the active route `:id` opened first so it's the active tab).

```ts
// addition to ngOnInit()
const otherTabsToOpen = url.open.filter((openId) => openId !== id);
for (const otherId of otherTabsToOpen) {
  // Sequential rather than Promise.all so the active tab settles first.
  await this.store.openTab(otherId);
}
// Then make sure the active tab is the route :id (in case opening other tabs flipped active)
this.store.setActive(id);
```

The URL-sync `effect()` from Phase 4.4 grows to include the tab list:
```ts
const otherIds = this.store
  .openTabs()
  .map((t) => t.id)
  .filter((tid) => tid !== this.store.activeId());
const patch = buildUrlPatch({ q, filters, open: otherIds });
```

No new spec for this task — Task 5.4 covers the integration.

Commit: `feat(web-portal): hydrate ?open= tabs on init + sync URL (Phase 5.2)`

---

## Task 5.3: Active-tab navigation + last-tab-closed handler

**Files (modify):**
- `map-viewer.page.ts` — add `onTabSelected(id)` and `onTabClosed(id)` handlers.

```ts
onTabSelected(id: string): void {
  // Switch the route to make the new id the active tab.
  void this.router.navigate(['/knowledge-maps', id], {
    queryParamsHandling: 'preserve',
    replaceUrl: false,
  });
}

onTabClosed(id: string): void {
  this.store.closeTab(id);
  const remaining = this.store.openTabs();
  if (remaining.length === 0) {
    void this.router.navigate(['/knowledge-maps']);
    return;
  }
  // If we just closed the active tab, route to whatever the store fell back to.
  if (this.store.activeId() && this.store.activeId() !== id) {
    const fallback = this.store.activeId() as string;
    void this.router.navigate(['/knowledge-maps', fallback], {
      queryParamsHandling: 'preserve',
      replaceUrl: false,
    });
  }
}
```

Subtlety: when the user switches tabs via the bar, we navigate (not just call `store.setActive`) so the URL `:id` updates. The route's `ngOnInit` doesn't re-fire on a same-component param change by default — we need to subscribe to `route.paramMap` and call `store.openTab` (which short-circuits if already open) + `store.setActive`. Add this in the page constructor:

```ts
constructor() {
  // ...existing URL-sync effect...
  this.route.paramMap.subscribe((params) => {
    const newId = params.get('id');
    if (newId && this.store.tabsById().has(newId)) {
      this.store.setActive(newId);
    }
  });
}
```

Commit: `feat(web-portal): tab navigation + last-tab-closed back to /knowledge-maps (Phase 5.3)`

---

## Task 5.4: Wire `<cce-tabs-bar>` into MapViewerPage + spec

**Files (modify + extend):**
- `map-viewer.page.html` — render `<cce-tabs-bar>` at the top of the active-tab branch (above the header).
- `map-viewer.page.spec.ts` — add 2 tests for tab open/close behavior.

Template addition:
```html
@if (store.activeTab()) {
  <cce-tabs-bar
    [tabs]="store.openTabs()"
    [activeId]="store.activeId()"
    [locale]="locale()"
    (tabSelected)="onTabSelected($event)"
    (tabClosed)="onTabClosed($event)"
  />
}
```

Tests (~2 new on the page spec):
1. URL `?open=m2,m3` opens the additional tabs after the active one.
2. Closing the last open tab navigates to `/knowledge-maps`.

Commit: `feat(web-portal): wire TabsBar into MapViewerPage + spec (Phase 5.4)`

---

## Phase 05 — completion checklist

- [ ] Task 5.1 — TabsBarComponent (~5 tests).
- [ ] Task 5.2 — `?open=` hydration + URL sync.
- [ ] Task 5.3 — Tab nav handlers + last-tab-closed.
- [ ] Task 5.4 — Wire into page + spec (~2 tests).
- [ ] All web-portal Jest tests passing.
- [ ] admin-cms still 218/218.
- [ ] Lint + build clean.

**If all boxes ticked, Phase 05 complete. Proceed to Phase 06 (Export menu).**
