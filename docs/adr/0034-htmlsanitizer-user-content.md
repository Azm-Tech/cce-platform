# ADR-0034 — HtmlSanitizer for User-Submitted Content

**Status:** Accepted
**Date:** 2026-04-29
**Deciders:** CCE backend team

---

## Context

The CCE platform allows users to submit community content: forum posts, replies, profile bios, and topic descriptions. This content is rendered as HTML in the browser. Without server-side sanitisation, a malicious user could inject `<script>` tags or event-handler attributes, enabling stored XSS attacks against other users.

Client-side sanitisation alone is not sufficient — it can be bypassed by direct API calls. The platform needs a **server-side** defence that is applied consistently regardless of the client.

Options considered:

| Option | Notes |
|---|---|
| Strip all HTML (plain text only) | Safe but too restrictive — loses formatting |
| Custom regex-based allowlist | Error-prone; regex is not a reliable HTML parser |
| `HtmlSanitizer` NuGet (mganss) | Battle-tested; allowlist-based; actively maintained |
| Microsoft AntiXSS | Older; less flexible allowlist configuration |

---

## Decision

Use **`HtmlSanitizer`** (NuGet package `HtmlSanitizer` by mganss) with a curated allowlist of safe elements and attributes.

Allowed elements:

```
p, br, strong, em, a (https:// href only), ul, ol, li, blockquote, code, pre
```

Allowed attributes:

- `href` on `<a>` — restricted to `https://` scheme only (no `javascript:`, no `data:`).
- No `style`, `class`, `id`, or event-handler attributes permitted.

Implementation:

- `IHtmlSanitizer` interface in `CCE.Application.Common.Sanitization`.
- `HtmlSanitizerWrapper` (Infrastructure) wraps the NuGet library and applies the allowlist.
- Registered as `AddSingleton<IHtmlSanitizer, HtmlSanitizerWrapper>()` in `DependencyInjection`.
- All `FluentValidator` classes on user-content commands (post body, reply content, profile bio, topic description) inject `IHtmlSanitizer` and call `Sanitize(input)` before the field passes validation.

---

## Consequences

- Server-side XSS defence is applied to all user-generated content regardless of the originating client.
- One NuGet dependency (`HtmlSanitizer`); the library has no transitive dependencies.
- The allowlist is conservative — legitimate formatting (bold, italic, links, lists, code blocks) is preserved; everything else is stripped.
- Future additions to the allowlist (e.g., `img` with `src` restricted to the CDN domain) require only a configuration change in `HtmlSanitizerWrapper`, not a schema migration.
- Content that was stored prior to sanitisation enforcement (pre-Phase 4 seeded data) is safe because it was created by the system seeder, not by users.
