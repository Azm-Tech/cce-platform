import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { AssistantApiService } from './assistant-api.service';

@Component({
  selector: 'cce-assistant-page',
  standalone: true,
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    MatButtonModule, MatFormFieldModule, MatIconModule,
    MatInputModule, MatProgressBarModule,
    TranslateModule,
  ],
  templateUrl: './assistant.page.html',
  styleUrl: './assistant.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AssistantPage {
  private readonly api = inject(AssistantApiService);
  private readonly localeService = inject(LocaleService);

  readonly question = new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(1)] });
  readonly reply = signal<string | null>(null);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  /** Pure-loading guard. Form validity is checked inside send() because
   *  reactive-form `valid` isn't a signal and computed() can't react to it. */
  readonly canSend = computed(() => !this.loading());

  async send(): Promise<void> {
    if (this.loading()) return;
    const q = this.question.value.trim();
    if (!q) return;
    this.loading.set(true);
    this.errorKind.set(null);
    this.reply.set(null);
    const res = await this.api.query({
      question: q,
      locale: this.localeService.locale(),
    });
    this.loading.set(false);
    if (res.ok) this.reply.set(res.value.reply);
    else this.errorKind.set(res.error.kind);
  }
}
