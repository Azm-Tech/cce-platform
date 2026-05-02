import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import { AssistantStore } from './assistant-store.service';

const MAX_LENGTH = 2000;
const WARN_LENGTH = 1500;

/**
 * Compose box — Reactive Forms textarea + send/cancel button. Enter
 * sends; Shift+Enter inserts a newline. While streaming, the send
 * button morphs into Cancel. Char counter visible at ≥1500; submit
 * disabled at >2000.
 */
@Component({
  selector: 'cce-compose-box',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule,
    TranslateModule,
  ],
  templateUrl: './compose-box.component.html',
  styleUrl: './compose-box.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ComposeBoxComponent {
  private readonly store = inject(AssistantStore);

  readonly textControl = new FormControl<string>('', { nonNullable: true });
  readonly value = toSignal(this.textControl.valueChanges, { initialValue: '' });

  readonly maxLength = MAX_LENGTH;
  readonly warnLength = WARN_LENGTH;

  readonly streaming = this.store.streaming;
  readonly canSend = computed(() => {
    if (this.streaming()) return false;
    const v = this.value();
    return v.trim() !== '' && v.length <= MAX_LENGTH;
  });
  readonly showCharCount = computed(() => this.value().length >= WARN_LENGTH);

  async send(): Promise<void> {
    if (!this.canSend()) return;
    const text = this.textControl.value;
    this.textControl.setValue('');
    await this.store.sendMessage(text);
  }

  cancel(): void {
    this.store.cancel();
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey && !event.isComposing) {
      event.preventDefault();
      void this.send();
    }
  }
}
