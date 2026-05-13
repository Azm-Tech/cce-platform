import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

/**
 * Unified page-hero header used at the top of public list pages
 * (news, events, community, resources, countries, knowledge maps).
 *
 * Visual design:
 *  - Brand-tinted card with a deep-green → mid-green → gold vertical
 *    stripe on the leading edge.
 *  - Optional eyebrow chip (small icon + ALL-CAPS label).
 *  - Gradient page title (deep-green to mid-green) for hierarchy.
 *  - Subtitle copy capped at ~60 ch.
 *  - Two optional content-projection slots:
 *     - `[hero-aside]` — toolbar/search/filter/CTA on the right
 *     - `[hero-art]`   — fully custom decorative art on the right
 *
 * Strings are passed in already-translated. Consumers handle i18n.
 *
 * @example
 * ```html
 * <cce-page-hero
 *   eyebrowIcon="newspaper"
 *   [eyebrow]="'nav.news' | translate"
 *   [title]="'news.title' | translate"
 *   [subtitle]="'news.subtitle' | translate"
 * >
 *   <button hero-aside mat-stroked-button>Filter</button>
 * </cce-page-hero>
 * ```
 */
@Component({
  selector: 'cce-page-hero',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="cce-page-hero">
      <div class="cce-page-hero__text">
        @if (eyebrow()) {
          <span class="cce-page-hero__eyebrow">
            @if (eyebrowIcon(); as ic) {
              <mat-icon aria-hidden="true">{{ ic }}</mat-icon>
            }
            {{ eyebrow() }}
          </span>
        }
        <h1 class="cce-page-hero__title">{{ title() }}</h1>
        @if (subtitle()) {
          <p class="cce-page-hero__subtitle">{{ subtitle() }}</p>
        }
      </div>

      <!-- Optional aside cluster (search / filters / counts). -->
      <div class="cce-page-hero__aside">
        <ng-content select="[hero-aside]" />
      </div>

      <!-- Optional fully-custom decorative art on the right. -->
      <div class="cce-page-hero__art" aria-hidden="true">
        <ng-content select="[hero-art]" />
      </div>
    </header>
  `,
  styles: [`
    :host {
      display: block;
      margin-bottom: 1.6rem;
    }

    .cce-page-hero {
      position: relative;
      display: grid;
      grid-template-columns: minmax(0, 1fr) auto;
      gap: clamp(0.75rem, 2vw, 1.5rem);
      align-items: center;
      padding: clamp(1.4rem, 3vw, 2.4rem) clamp(1.4rem, 3vw, 2.6rem);
      padding-inline-start: calc(clamp(1.4rem, 3vw, 2.6rem) + 4px);
      background:
        radial-gradient(900px 240px at 0% 0%, rgba(20, 184, 143, 0.12), transparent 60%),
        linear-gradient(135deg, #ffffff 0%, #f4faf7 100%);
      border: 1px solid rgba(0, 108, 79, 0.10);
      border-radius: 24px;
      overflow: hidden;
      isolation: isolate;
    }

    /* Brand-gradient leading edge stripe. */
    .cce-page-hero::before {
      content: '';
      position: absolute;
      inset-inline-start: 0;
      top: 0;
      bottom: 0;
      width: 4px;
      background: linear-gradient(180deg, #006c4f 0%, #14b88f 50%, #c8a045 100%);
    }

    .cce-page-hero__text { min-width: 0; }

    .cce-page-hero__eyebrow {
      display: inline-flex;
      align-items: center;
      gap: 0.4rem;
      font-size: 0.74rem;
      font-weight: 700;
      letter-spacing: 0.1em;
      text-transform: uppercase;
      color: #006c4f;
      background: rgba(0, 108, 79, 0.08);
      padding: 0.32rem 0.72rem;
      border-radius: 999px;
      margin-bottom: 0.85rem;

      mat-icon {
        font-size: 14px;
        width: 14px;
        height: 14px;
      }
    }

    .cce-page-hero__title {
      margin: 0 0 0.55rem;
      font-size: clamp(1.5rem, 2.4vw, 2.25rem);
      line-height: 1.1;
      font-weight: 800;
      letter-spacing: -0.02em;
      background: linear-gradient(135deg, #003a2b 0%, #006c4f 50%, #0f8b6c 100%);
      -webkit-background-clip: text;
              background-clip: text;
      color: transparent;
    }

    .cce-page-hero__subtitle {
      margin: 0;
      font-size: clamp(0.94rem, 1.05vw, 1.05rem);
      line-height: 1.55;
      color: rgba(28, 39, 36, 0.72);
      max-width: 60ch;
    }

    .cce-page-hero__aside {
      display: flex;
      align-items: center;
      gap: 0.6rem;
      flex-wrap: wrap;
      justify-content: flex-end;

      /* Hide entirely when no content is projected. CSS :empty matches
       * elements with no children (and no text). */
      &:empty { display: none; }
    }

    .cce-page-hero__art {
      position: relative;
      &:empty { display: none; }
    }

    /* On narrow screens stack the aside/art beneath the text. */
    @media (max-width: 720px) {
      .cce-page-hero {
        grid-template-columns: 1fr;
      }
      .cce-page-hero__aside {
        justify-content: flex-start;
      }
    }
  `],
})
export class PageHeroComponent {
  /** Optional Material icon name shown in the eyebrow chip. */
  readonly eyebrowIcon = input<string | null>(null);
  /** Already-translated eyebrow label (small ALL-CAPS chip). */
  readonly eyebrow = input<string>('');
  /** Already-translated H1 page title. Required. */
  readonly title = input.required<string>();
  /** Already-translated subtitle copy. Optional. */
  readonly subtitle = input<string>('');
}
