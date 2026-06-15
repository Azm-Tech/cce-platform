
import { ChangeDetectionStrategy, Component, OnInit, inject, input, output, signal } from '@angular/core';
import {
  FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators,
} from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { CommunityApiService } from './community-api.service';
import type { CreateReplyPayload } from './community.types';

interface ComposeReplyFormShape {
  content: FormControl<string>;
  locale: FormControl<'ar' | 'en'>;
}

@Component({
  selector: 'cce-compose-reply-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatIconModule,
    TranslocoModule,
  ],
  templateUrl: './compose-reply-form.component.html',
  styleUrl: './compose-reply-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ComposeReplyFormComponent implements OnInit {
  private readonly api = inject(CommunityApiService);
  private readonly toast = inject(ToastService);
  private readonly localeService = inject(LocaleService);
  private readonly fb = inject(FormBuilder);

  readonly postId = input.required<string>();
  readonly parentReplyId = input<string | null>(null);
  readonly parentHandle = input<string | null>(null);
  readonly cancelReply = output<void>();
  readonly replyCreated = output<string>();

  readonly submitting = signal(false);
  readonly errorKind = signal<string | null>(null);

  readonly form: FormGroup<ComposeReplyFormShape> = this.fb.nonNullable.group({
    content: this.fb.nonNullable.control('', [
      Validators.required,
      Validators.minLength(1),
      Validators.maxLength(500),
    ]),
    locale: this.fb.nonNullable.control<'ar' | 'en'>('en', Validators.required),
  });

  ngOnInit(): void {
    this.form.controls.locale.setValue(this.localeService.locale());
  }

  async submit(): Promise<void> {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const payload: CreateReplyPayload = {
      content: v.content.trim(),
      locale: v.locale,
      parentReplyId: this.parentReplyId() ?? null,
    };
    this.submitting.set(true);
    this.errorKind.set(null);
    const res = await this.api.createReply(this.postId(), payload);
    this.submitting.set(false);
    if (res.ok) {
      this.toast.success('community.reply.toast');
      this.form.reset({ content: '', locale: this.localeService.locale() });
      this.replyCreated.emit(res.value.id);
    } else {
      this.errorKind.set(res.error.kind);
    }
  }
}
