# Phase 08 — Community

> Parent: [`../2026-04-30-web-portal.md`](../2026-04-30-web-portal.md) · Spec: [`../../specs/2026-04-30-web-portal-design.md`](../../specs/2026-04-30-web-portal-design.md) §5 (`/api/topics`, `/api/community/*`)

**Phase goal:** Public users browse community topics + posts; authenticated users compose posts, reply, rate, and (post authors only) mark a reply as the accepted answer. Anonymous users see "Sign in to post" CTAs in place of write controls.

**Tasks:** 9
**Working directory:** `/Users/m/CCE/`
**Preconditions:**
- Phase 07 closed (`f4bfe8b`).
- AuthService, authGuard, FollowsRegistryService, `[cceFollow]` directive all in place.

---

## Endpoint coverage

| Endpoint | Method | Phase 08 surface | Auth |
|---|---|---|---|
| `/api/topics` | GET | Task 8.2 (TopicsListPage) | ✓ |
| `/api/community/topics/{slug}` | GET | Task 8.3 (TopicDetailPage header) | ✓ |
| `/api/community/topics/{id}/posts` | GET (paged) | Task 8.3 (post list inside topic) | ✓ |
| `/api/community/posts/{id}` | GET | Task 8.4 (PostDetailPage) | ✓ |
| `/api/community/posts/{id}/replies` | GET (paged) | Task 8.4 (replies thread) | ✓ |
| `/api/community/posts` | POST | Task 8.5 (ComposePost) | ✗ |
| `/api/community/posts/{id}/replies` | POST | Task 8.6 (ComposeReply) | ✗ |
| `/api/community/posts/{id}/rate` | POST | Task 8.7 (RatePostControl) | ✗ |
| `/api/community/posts/{id}/mark-answer` | POST | Task 8.7 (MarkAnswerButton) | ✗ |
| `/api/community/replies/{id}` | PUT | Task 8.6 (edit own reply, optional v0.1.0) | ✗ |

**Backend contract notes** (verified against `CommunityPublicEndpoints.cs`, `CommunityWriteEndpoints.cs`, `Community/Public/Dtos/*.cs`, `TopicsPublicEndpoints.cs`):

```csharp
// PublicTopicDto — bilingual metadata (name/description in both locales)
public sealed record PublicTopicDto(
    Guid Id,
    string NameAr, string NameEn,
    string DescriptionAr, string DescriptionEn,
    string Slug,
    Guid? ParentId,             // tree structure; v0.1.0 renders flat
    string? IconUrl,
    int OrderIndex);

// PublicPostDto — Content is a SINGLE string per post in the post's Locale
public sealed record PublicPostDto(
    Guid Id, Guid TopicId, Guid AuthorId,
    string Content, string Locale,    // "ar" | "en" — one locale per post
    bool IsAnswerable,
    Guid? AnsweredReplyId,
    DateTimeOffset CreatedOn);

// PublicPostReplyDto — Content + Locale per reply
public sealed record PublicPostReplyDto(
    Guid Id, Guid PostId, Guid AuthorId,
    string Content, string Locale,
    Guid? ParentReplyId,              // optional nested replies
    bool IsByExpert,                  // server-computed flag
    DateTimeOffset CreatedOn);

// Write payloads
public sealed record CreatePostRequest(Guid TopicId, string Content, string Locale, bool IsAnswerable);
public sealed record CreateReplyRequest(string Content, string Locale, Guid? ParentReplyId);
public sealed record RatePostRequest(int Stars);                  // 1..5
public sealed record MarkAnswerRequest(Guid ReplyId);
public sealed record EditReplyRequest(string Content);
```

**Important contract details baked into v0.1.0:**

1. **Posts and replies are single-locale content.** `Content` is one string in the locale specified by `Locale`. The FE composer asks the user to pick a locale (defaults to current LocaleService value); we render `content` as-is regardless of the active LocaleService — but mark posts written in the *other* locale with a small "in {{locale}}" badge. Cross-language threads are normal in this codebase.
2. **The list-topics endpoint is `/api/topics`, NOT `/api/community/topics`.** This is a backend URL inconsistency we just live with for v0.1.0.
3. **`MarkPostAnswered` is author-only** (server-side enforced). FE renders the "Mark as answer" button only when `currentUser().id === post.authorId`. UI-side guard is convenience; the real guard is server.
4. **`PublicPostDto` does not embed author display name.** The FE shows the GUID until Phase 9 hydration, same compromise we made in Phase 07's FollowsPage.

## Hand-defined DTOs

```ts
// frontend/apps/web-portal/src/app/features/community/community.types.ts
import type { PagedResult } from '../knowledge-center/shared.types';

export interface PublicTopic {
  id: string;
  nameAr: string; nameEn: string;
  descriptionAr: string; descriptionEn: string;
  slug: string;
  parentId: string | null;
  iconUrl: string | null;
  orderIndex: number;
}

export interface PublicPost {
  id: string;
  topicId: string;
  authorId: string;
  content: string;
  locale: 'ar' | 'en';
  isAnswerable: boolean;
  answeredReplyId: string | null;
  createdOn: string;
}

export interface PublicPostReply {
  id: string;
  postId: string;
  authorId: string;
  content: string;
  locale: 'ar' | 'en';
  parentReplyId: string | null;
  isByExpert: boolean;
  createdOn: string;
}

export interface CreatePostPayload {
  topicId: string;
  content: string;
  locale: 'ar' | 'en';
  isAnswerable: boolean;
}

export interface CreateReplyPayload {
  content: string;
  locale: 'ar' | 'en';
  parentReplyId?: string | null;
}

export type { PagedResult };
```

## Folder structure

```
apps/web-portal/src/app/features/community/
├── community.types.ts
├── community-api.service.{ts,spec.ts}              # Task 8.1 (one service for all 10 endpoints)
├── topics-list.page.{ts,html,scss,spec.ts}         # Task 8.2 (mounted at /community)
├── topic-card.component.{ts,scss}                  # presentation (inline template)
├── topic-detail.page.{ts,html,scss,spec.ts}        # Task 8.3 (/community/topics/:slug)
├── post-summary.component.{ts,scss}                # row-style presentation
├── post-detail.page.{ts,html,scss,spec.ts}         # Task 8.4 (/community/posts/:id)
├── reply.component.{ts,scss}                       # presentation card
├── compose-post-dialog.component.{ts,html,scss,spec.ts}  # Task 8.5 (mat-dialog)
├── compose-reply-form.component.{ts,html,scss,spec.ts}   # Task 8.6 (inline on post detail)
├── rate-post-control.component.{ts,html,scss,spec.ts}    # Task 8.7 (1-5 stars)
├── mark-answer-button.component.{ts,html,scss,spec.ts}   # Task 8.7 (author-only)
├── sign-in-cta.component.{ts,scss}                 # Task 8.8 (anonymous write affordance)
└── routes.ts                                       # Task 8.9
```

---

## Task 8.1: CommunityApiService + types

**Files (all new):**
- `features/community/community.types.ts`
- `features/community/community-api.service.{ts,spec.ts}`

CommunityApiService methods:
- `listTopics()` → `Result<PublicTopic[]>` (GET `/api/topics`).
- `getTopicBySlug(slug)` → `Result<PublicTopic>` (GET `/api/community/topics/{slug}`); 404 → `not-found`.
- `listPosts(topicId, { page?, pageSize? })` → `Result<PagedResult<PublicPost>>`.
- `getPost(id)` → `Result<PublicPost>` (GET `/api/community/posts/{id}`); 404 → `not-found`.
- `listReplies(postId, { page?, pageSize? })` → `Result<PagedResult<PublicPostReply>>`.
- `createPost(payload)` → `Result<{ id: string }>` (POST `/api/community/posts`).
- `createReply(postId, payload)` → `Result<{ id: string }>` (POST `/api/community/posts/{id}/replies`).
- `ratePost(postId, stars)` → `Result<void>` (POST `/api/community/posts/{id}/rate`).
- `markAnswer(postId, replyId)` → `Result<void>` (POST `/api/community/posts/{id}/mark-answer`).
- `editReply(replyId, content)` → `Result<void>` (PUT `/api/community/replies/{id}`).

**Tests (~10):**
1. `listTopics()` GETs `/api/topics`.
2. `getTopicBySlug('jo')` GETs `/api/community/topics/jo`; 404 returns not-found.
3. `listPosts('t1', { page: 2 })` GETs `/api/community/topics/t1/posts?page=2`.
4. `getPost('p1')` GETs and returns the DTO.
5. `listReplies('p1', {})` GETs `/api/community/posts/p1/replies`.
6. `createPost(payload)` POSTs body unchanged + returns the new id.
7. `createReply('p1', payload)` POSTs to `/api/community/posts/p1/replies`.
8. `ratePost('p1', 5)` POSTs `{ stars: 5 }` to `/api/community/posts/p1/rate`.
9. `markAnswer('p1', 'r1')` POSTs `{ replyId: 'r1' }`.
10. `editReply('r1', 'new content')` PUTs `{ content: ... }` to `/api/community/replies/r1`.

Commit: `feat(web-portal): CommunityApiService + DTOs (Phase 8.1)`

---

## Task 8.2: TopicsListPage at /community

**Files:**
- `features/community/topics-list.page.{ts,html,scss,spec.ts}`
- `features/community/topic-card.component.{ts,scss}` (inline template)

TopicCardComponent: signal-input pattern (`input.required<PublicTopic>`, `input<'ar'|'en'>('en')`). Renders icon (when set), localized name, localized description (160-char excerpt), routerLink to `/community/topics/{slug}`. Includes `[cceFollow] entityType="topic"` follow-button on the card so users can follow/unfollow without entering the topic.

TopicsListPage:
- Public route. Loads via `community.listTopics()`. Sorts by `orderIndex`.
- Renders a Bootstrap grid of `<cce-topic-card>`. v0.1.0 renders all topics flat (no parentId tree).
- Empty / error / loading states.

Tests (~5):
1. Init load renders one card per topic, sorted by orderIndex ascending.
2. Card title localizes when locale toggles.
3. Empty result renders "No topics yet" message.
4. Error path renders error banner with retry.
5. Card routerLink uses topic.slug.

Commit: `feat(web-portal): TopicsListPage at /community (Phase 8.2)`

---

## Task 8.3: TopicDetailPage at /community/topics/:slug

**Files:**
- `features/community/topic-detail.page.{ts,html,scss,spec.ts}`
- `features/community/post-summary.component.{ts,scss}` (inline template)

PostSummaryComponent: row-style card. `input.required<PublicPost>`. Renders content excerpt (160 chars), language badge (when locale differs from current), createdOn date, "Answered ✓" pill when `answeredReplyId` is set, routerLink to `/community/posts/{id}`.

TopicDetailPage:
- Reads `:slug` from route. Calls `getTopicBySlug` AND `listPosts(topicId, ...)` in parallel using the slug-to-id resolution: first await `getTopicBySlug`, then call `listPosts(topic.id)`.
- Renders topic header (icon + name + description, locale-driven), follow toggle (`[cceFollow] entityType="topic" entityId="{topic.id}"`), "New post" button (opens ComposePostDialog from Task 8.5; replaced with SignInCta when anonymous, see Task 8.8).
- Below header: paged list of `<cce-post-summary>`. Pagination via `mat-paginator`.
- 404 on getTopicBySlug → not-found block.
- After successful post creation (dialog closes with `{ submitted: true }`), refreshes the list.

Tests (~6):
1. Init: getTopicBySlug then listPosts called with the resolved id.
2. 404 on getTopicBySlug renders not-found block.
3. Topic header localizes name + description.
4. Pagination change re-fires listPosts.
5. New-post click opens ComposePostDialog (mocked dialog assertion).
6. Successful submit refreshes the list.

Commit: `feat(web-portal): TopicDetailPage at /community/topics/:slug (Phase 8.3)`

---

## Task 8.4: PostDetailPage at /community/posts/:id

**Files:**
- `features/community/post-detail.page.{ts,html,scss,spec.ts}`
- `features/community/reply.component.{ts,scss}` (inline template)

ReplyComponent: presentation card. `input.required<PublicPostReply>`. Renders content (raw + paragraphs preserved via `white-space: pre-wrap`), language badge, expert badge when `isByExpert`, createdOn date, "Mark as answer" button slot (rendered only when the host page passes `isMarkable=true` — wired via signal-input).

PostDetailPage:
- Reads `:id` from route. Loads `getPost(id)` AND `listReplies(id, ...)` in parallel.
- Renders post header (author id chip — Phase 9 hydration), content (pre-wrap), language badge, follow toggle (`[cceFollow] entityType="post"`), rate-post control (Task 8.7), "Add reply" form (Task 8.6) or sign-in CTA when anonymous.
- Below post: list of replies. The reply with `id === post.answeredReplyId` is rendered first with an "Accepted answer" green border + checkmark badge.
- "Mark as answer" button on each reply is shown only when `currentUser?.id === post.authorId` AND `post.isAnswerable` AND `post.answeredReplyId !== reply.id`. Clicking calls `markAnswer(post.id, reply.id)`, refreshes the post + replies.
- Pagination via `mat-paginator` for replies.

Tests (~6):
1. Init load: getPost + listReplies in parallel; binds to DOM.
2. 404 on getPost → not-found block.
3. Accepted answer is hoisted to first position with badge.
4. Mark-as-answer button visible only when current user is post author and post is answerable.
5. Mark-as-answer click calls markAnswer + refreshes the post.
6. Pagination on replies fires listReplies again.

Commit: `feat(web-portal): PostDetailPage with replies thread (Phase 8.4)`

---

## Task 8.5: ComposePostDialog

**Files:**
- `features/community/compose-post-dialog.component.{ts,html,scss,spec.ts}`

ComposePostDialogComponent: opened from TopicDetailPage's "New post" button. `MAT_DIALOG_DATA: { topicId: string }`.
- Form (typed Reactive Form):
  - `content` (textarea, required, 10..5000 chars).
  - `locale` (radio: ar / en, default = current LocaleService locale).
  - `isAnswerable` (mat-checkbox, default true — "Allow replies to be marked as answer").
- Submit calls `createPost({ topicId, content, locale, isAnswerable })`. On success: `dialogRef.close({ submitted: true, postId })` + toast.
- On error: keep dialog open, show inline banner.

Tests (~5):
1. Form submit posts payload with topicId from dialog data.
2. Form invalid when content is shorter than 10 chars (submit short-circuits).
3. Locale defaults to LocaleService.locale().
4. On success: dialogRef.close({ submitted: true, postId }) + toast.success.
5. On error: dialog stays open, errorKind signal set.

Commit: `feat(web-portal): ComposePostDialog (Phase 8.5)`

---

## Task 8.6: ComposeReplyForm

**Files:**
- `features/community/compose-reply-form.component.{ts,html,scss,spec.ts}`

ComposeReplyFormComponent: inline form rendered below the post on PostDetailPage when authenticated. Single-emit child:
- `input postId: string` — required.
- Output: `(replyCreated)` emits the new reply's id; PostDetailPage handles by refreshing the replies list.
- Form: content (textarea, 1..5000 chars), locale (radio default current locale).
- "Reply" button calls `createReply(postId, { content, locale })`; on success clears form + emits.

Optional v0.1.0 extension: edit-own-reply via inline edit button on `<cce-reply>` rows. Phase 9 polish if not delivered.

Tests (~5):
1. Submit calls createReply(postId, { content, locale }).
2. Form invalid when content empty (submit short-circuits).
3. Locale defaults to current LocaleService locale.
4. On success: clears form + emits replyCreated(id).
5. On error: keeps form populated + shows inline error.

Commit: `feat(web-portal): ComposeReplyForm (Phase 8.6)`

---

## Task 8.7: RatePostControl + MarkAnswerButton

**Files:**
- `features/community/rate-post-control.component.{ts,html,scss,spec.ts}`
- `features/community/mark-answer-button.component.{ts,html,scss,spec.ts}`

RatePostControlComponent:
- `input postId: string` — required.
- `input currentUserRating: number | null` (signal input, default null) — passed in from parent if known.
- 1-5 star widget (radiogroup), keyboard-accessible (Tab / Enter), aria-label per star (reuse the pattern from Phase 6.6 ServiceRatingDialog if the DOM is shared — likely same SCSS).
- Click on a star calls `ratePost(postId, stars)`. On success: toast.success "Rated."
- Anonymous users get a sign-in CTA placeholder instead of the widget.

MarkAnswerButtonComponent:
- Inputs `postId: string`, `replyId: string`, `disabled: boolean`.
- Renders a `mat-button` with check icon.
- Click calls `markAnswer(postId, replyId)`; emits `(marked)` after success so parent refreshes.

Tests (~5 total):
1. RatePostControl: 5-star click calls ratePost(postId, 5).
2. RatePostControl: anonymous renders SignInCta instead of stars.
3. RatePostControl: keyboard Enter on a star also fires the rate call.
4. MarkAnswerButton: click calls markAnswer(postId, replyId) and emits marked.
5. MarkAnswerButton: when disabled, click is a no-op.

Commit: `feat(web-portal): RatePostControl + MarkAnswerButton (Phase 8.7)`

---

## Task 8.8: SignInCta — anonymous-friendly write affordances

**Files:**
- `features/community/sign-in-cta.component.{ts,scss}`

SignInCtaComponent: tiny presentation block. `input message: string` (i18n key, default "community.signInToPost"). Renders message + a "Sign in" button that calls `auth.signIn(currentUrl)` with the current pathname so the user lands back on the same page after auth.

Replaces the compose / rate / reply controls when `auth.isAuthenticated() === false`. Used by:
- TopicDetailPage (replaces "New post" button).
- PostDetailPage (replaces ComposeReplyForm + RatePostControl).

Tests (~3):
1. Click on "Sign in" button calls auth.signIn(currentUrl).
2. Default i18n key renders "Sign in to post".
3. Custom message input overrides the default.

Commit: `feat(web-portal): SignInCta for anonymous community pages (Phase 8.8)`

---

## Task 8.9: Routes + i18n + E2E nav smoke

**Files:**
- New: `features/community/routes.ts`.
- Modify: `apps/web-portal/src/app/app.routes.ts` — add `/community`.
- Modify: `libs/i18n/src/lib/i18n/{en,ar}.json` — extend.
- New: `apps/web-portal-e2e/src/community.spec.ts`.

`features/community/routes.ts`:

```ts
export const COMMUNITY_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./topics-list.page').then((m) => m.TopicsListPage) },
  { path: 'topics/:slug', loadComponent: () => import('./topic-detail.page').then((m) => m.TopicDetailPage) },
  { path: 'posts/:id', loadComponent: () => import('./post-detail.page').then((m) => m.PostDetailPage) },
];
```

Add to `app.routes.ts`:

```ts
{
  path: 'community',
  loadChildren: () => import('./features/community/routes').then((m) => m.COMMUNITY_ROUTES),
  title: 'CCE — Community',
},
```

Header nav: `nav.community` already exists (added in Phase 0 nav-config).

i18n additions (en + ar mirrored):

`community.title` — "Community"
`community.empty.topics` — "No topics yet."
`community.empty.posts` — "No posts in this topic yet. Be the first."
`community.empty.replies` — "No replies yet."
`community.newPostButton` — "New post"
`community.signInToPost` — "Sign in to post."
`community.signInToReply` — "Sign in to reply."
`community.signInToRate` — "Sign in to rate."
`community.signInButton` — "Sign in"
`community.acceptedAnswer` — "Accepted answer"
`community.expertBadge` — "Expert"
`community.languageBadge` — "in {{locale}}"
`community.notFound` — "Topic / post not found."

ComposePostDialog:
- `community.compose.title` — "Compose new post"
- `community.compose.content` — "Content"
- `community.compose.locale` — "Language"
- `community.compose.isAnswerable` — "Allow replies to be marked as accepted answer"
- `community.compose.submit` — "Post"
- `community.compose.toast` — "Post created."

ComposeReplyForm:
- `community.reply.placeholder` — "Write a reply…"
- `community.reply.submit` — "Reply"
- `community.reply.toast` — "Reply posted."

RatePostControl + MarkAnswerButton:
- `community.rate.toast` — "Rated."
- `community.markAnswer.button` — "Mark as accepted answer"
- `community.markAnswer.toast` — "Marked as accepted answer."

E2E nav smoke at `apps/web-portal-e2e/src/community.spec.ts`:
- Header → /community attaches `cce-topics-list-page`.
- Anonymous user on /community sees topics (smoke; full data verified in Phase 9 close-out).
- /community/topics/some-slug + /community/posts/some-id mount correctly even when 404 (verifies routing only).

Commit: `feat(web-portal): /community routes + i18n + E2E (Phase 8.9)`

---

## Phase 08 — completion checklist

- [ ] Task 8.1 — CommunityApiService + DTOs (~10 tests).
- [ ] Task 8.2 — TopicsListPage (~5 tests).
- [ ] Task 8.3 — TopicDetailPage (~6 tests).
- [ ] Task 8.4 — PostDetailPage with replies (~6 tests).
- [ ] Task 8.5 — ComposePostDialog (~5 tests).
- [ ] Task 8.6 — ComposeReplyForm (~5 tests).
- [ ] Task 8.7 — RatePostControl + MarkAnswerButton (~5 tests).
- [ ] Task 8.8 — SignInCta (~3 tests).
- [ ] Task 8.9 — Routes + i18n + E2E.
- [ ] All Jest tests passing.
- [ ] admin-cms still 218/218.
- [ ] Lint + build clean.

**If all boxes ticked, Phase 08 complete. Proceed to Phase 09 (Skeleton + close-out).**

---

## Phase 9 polish backlog

- **Author-name hydration** — posts/replies render `authorId` as a GUID chip; Phase 9 should hydrate via a small users-list endpoint (TBD if backend grows one) or via expert-profile lookup for `isByExpert: true` cases.
- **Threaded replies** — `parentReplyId` is captured in DTOs but v0.1.0 renders flat. Phase 9 polish or v0.2.0 to add visual indentation + collapse/expand.
- **Edit-own-reply** — endpoint exists (`PUT /api/community/replies/{id}`); v0.1.0 may ship a basic inline edit on reply rows. If it slips, Phase 9 backlog.
- **Soft-delete reply** — backend has `EditReply` but no delete-reply on the **public** endpoints group. The admin moderation does soft-deletes (Sub-5). Public users can't delete their own replies in v0.1.0.
- **Topic tree (parentId)** — v0.1.0 renders flat; topic hierarchy ships in Phase 9 polish.
