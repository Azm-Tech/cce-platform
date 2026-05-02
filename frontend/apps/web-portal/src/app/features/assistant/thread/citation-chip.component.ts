import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslateModule } from '@ngx-translate/core';
import type { Citation } from '../assistant.types';

/**
 * Citation chip — renders an inline numeric marker (`[N]`) or a
 * footer chip with title + kind icon. Tapping opens the source via
 * RouterLink. Hover shows the title + optional sourceText excerpt.
 */
@Component({
  selector: 'cce-citation-chip',
  standalone: true,
  imports: [RouterLink, MatIconModule, MatTooltipModule, TranslateModule],
  templateUrl: './citation-chip.component.html',
  styleUrl: './citation-chip.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CitationChipComponent {
  readonly citation = input.required<Citation>();
  readonly index = input.required<number>();
  /** 'inline' = `[N]` only; 'footer' = `[N] Title` with icon. */
  readonly variant = input<'inline' | 'footer'>('footer');

  iconName(): string {
    return this.citation().kind === 'resource' ? 'description' : 'account_tree';
  }

  tooltip(): string {
    const c = this.citation();
    return c.sourceText ? `${c.title} — ${c.sourceText}` : c.title;
  }
}
