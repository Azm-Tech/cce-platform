# ADR-0042 — Anonymous-friendly write affordances on community pages

**Status:** Accepted
**Date:** 2026-05-01
**Deciders:** CCE frontend team

---

## Context

The web-portal lets anonymous users browse the community freely (topics, posts, replies). Some affordances on these pages are **write actions** that require authentication:

- "New post" button on TopicDetailPage.
- Reply form on PostDetailPage.
- 1-5 star rating on a post.
- "Mark as accepted answer" button on a reply.

Three options for handling anonymous users on these affordances:

| Option | Pros | Cons |
|---|---|---|
| **Hide write controls** | Simplest UI | Anonymous user has no idea they could rate / reply if they signed in |
| **Show controls, redirect to sign-in on click** | Discoverability | Surprise redirect breaks the user's place in the page; they lose the post they were typing |
| **Show controls, replaced with inline "Sign in to X" CTA** | Discoverability + transparency + preserves context | Slightly more components to maintain |

---

## Decision

**Use option 3: when the user is anonymous, the write control's slot renders an inline `cce-sign-in-cta` block instead of the control itself. The CTA's "Sign in" button calls `auth.signIn(currentUrl)` so the user lands back on the same page after auth.**

Concretely:

- `SignInCtaComponent` lives at `features/community/sign-in-cta.component.ts`. Single signal-input `messageKey` (default `community.signInToPost`) lets each call site customize the copy ("Sign in to post.", "Sign in to reply.", "Sign in to rate.").
- TopicDetailPage's action row: `if (isAuthenticated()) { … "New post" button … } else { <cce-sign-in-cta messageKey="community.signInToPost" /> }`.
- PostDetailPage's compose row: replies form OR sign-in CTA.
- RatePostControl encapsulates the same pattern internally (renders stars OR sign-in CTA).

The CTA is a small dashed-border block that's visually distinct from a real button — it tells the user "this is locked behind sign-in" without hiding the affordance entirely.

## Consequences

**Positive:**
- Anonymous users discover what's available behind sign-in.
- They don't lose context: clicking "Sign in" returns them to the same page (return-URL preserved).
- Each call site can localize the message.

**Negative:**
- Slightly more visual noise on community pages for anonymous users (one CTA per gated control).
- The pattern requires a separate component instead of a one-liner — but the component is ~15 lines and reused 4× in Sub-6.

**Neutral:**
- The pattern complements the BFF cookie auth model (ADR-0039): the full-page sign-in redirect picks up the return URL automatically.
- Sub-7 community write features can reuse SignInCtaComponent without modification.
