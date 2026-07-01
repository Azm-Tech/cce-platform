import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { ConfirmDialogService } from '../feedback/confirm-dialog.service';
import { ToastService } from '../feedback/toast.service';
import { TranslationService } from './translation.service';
import type { TranslateFormat } from './translation.contracts';

/**
 * Small "Translate from Arabic" action placed above an English field. Machine-
 * translates the bound Arabic `source` (plain text or rich HTML) and emits the
 * result via `(translated)` for the parent to write into its `*En` control.
 *
 * Renders only in an Arabic UI when the Arabic source has content (AR→EN only).
 * If the English target already has content, asks to confirm overwrite first.
 */
@Component({
  selector: 'cce-translate-field',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, MatProgressSpinnerModule, TranslocoModule],
  template: `
    @if (visible()) {
      <button
        type="button"
        mat-button
        class="cce-translate-field__btn"
        [disabled]="loading() || !hasSource()"
        (click)="run()"
      >
        @if (loading()) {
          <mat-progress-spinner diameter="16" mode="indeterminate" />
        } @else {
          <mat-icon>auto_awesome</mat-icon>
        }
        {{ 'common.actions.translateFromArabic' | transloco }}
      </button>
    }
  `,
  styles: [
    `
      :host {
        display: flex;
        justify-content: flex-end;
      }

      .cce-translate-field__btn {
        font-size: 0.78rem;
        line-height: 1.4;
        min-height: 28px;
        padding: 0 8px;
        color: var(--color-brand);
      }

      .cce-translate-field__btn mat-progress-spinner {
        display: inline-block;
        margin-inline-end: 6px;
      }
    `,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TranslateFieldComponent {
  private readonly locale = inject(LocaleService).locale;
  private readonly translation = inject(TranslationService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmDialogService);

  /** The Arabic source value to translate. */
  readonly source = input('');
  /** Plain text or rich HTML — selects the translation prompt. */
  readonly format = input<TranslateFormat>('text');
  /** Whether the English target already has content (drives overwrite confirm). */
  readonly targetHasContent = input(false);

  /** Emits the translated English string. */
  readonly translated = output<string>();

  readonly loading = signal(false);

  /** AR→EN only: the action is shown in an Arabic UI… */
  readonly visible = computed(() => this.locale() === 'ar');
  /** …but stays disabled until there's Arabic text to translate. */
  readonly hasSource = computed(() => this.source().trim().length > 0);

  async run(): Promise<void> {
    if (this.loading() || !this.hasSource()) return;
    if (this.targetHasContent()) {
      const proceed = await this.confirm.confirm({
        titleKey: 'common.translate.confirmTitle',
        messageKey: 'common.translate.confirmOverwrite',
        confirmKey: 'common.translate.confirmAction',
      });
      if (!proceed) return;
    }
    this.loading.set(true);
    const res = await this.translation.translate(this.source(), { format: this.format() });
    this.loading.set(false);
    if (res.ok) this.translated.emit(res.text);
    else this.toast.error(`errors.${res.kind}`);
  }
}
