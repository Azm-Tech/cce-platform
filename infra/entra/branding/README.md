# CCE Entra ID branding assets

Drop the following files here before running `Configure-Branding.ps1`:

- `banner.png` — 280×60 px max, < 50 KB. Shown above the username field on the sign-in page.
- `square.png` — 240×240 px, < 50 KB. Shown when "stay signed in" page renders.
- `background.png` — 1920×1080 recommended, < 300 KB. Background of the sign-in page.
- `custom.css` — < 25 KB. Optional; overrides default sign-in CSS. Copy `custom.css.example` as a starting point.

These files are gitignored to keep brand assets out of the repo. Source-of-truth lives in the design system (see `frontend/libs/ui-kit/`). Operators copy the rendered assets here before running the script.

Sizing + format guidance: <https://learn.microsoft.com/entra/fundamentals/how-to-customize-branding>

## Important: rendering scope

Custom branding renders **only for users signing in to the CCE home tenant**. Multi-tenant partner-organization users see their own home-tenant sign-in page — this is a hard Entra ID security boundary, not a configuration choice.

## P1/P2 requirement

The Graph `organizationalBranding` API requires Entra ID P1 or P2 SKU. `Configure-Branding.ps1` detects missing licensing and exits 0 with a warning rather than failing.
