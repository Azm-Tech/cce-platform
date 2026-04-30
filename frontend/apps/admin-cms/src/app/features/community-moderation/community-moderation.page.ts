import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TranslateModule } from '@ngx-translate/core';
import { ConfirmDialogService } from '../../core/ui/confirm-dialog.service';
import { ToastService } from '../../core/ui/toast.service';
import { TaxonomyApiService } from '../taxonomies/taxonomy-api.service';

const GUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

interface ModerationForm {
  postId: FormControl<string>;
  replyId: FormControl<string>;
}

/**
 * Admin → Community moderation. The Internal API only exposes soft-delete
 * endpoints (no list of pending content), so v0.1.0 ships a power-user
 * by-ID form. Future phases can add a flag-queue once Sub-3 exposes one.
 */
@Component({
  selector: 'cce-community-moderation',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatButtonModule, MatCardModule, MatFormFieldModule, MatInputModule, TranslateModule,
  ],
  templateUrl: './community-moderation.page.html',
  styleUrl: './community-moderation.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CommunityModerationPage {
  private readonly api = inject(TaxonomyApiService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmDialogService);

  readonly form = new FormGroup<ModerationForm>({
    postId: new FormControl('', { nonNullable: true, validators: [Validators.pattern(GUID_RE)] }),
    replyId: new FormControl('', { nonNullable: true, validators: [Validators.pattern(GUID_RE)] }),
  });
  readonly busy = signal(false);

  async deletePost(): Promise<void> {
    const id = this.form.controls.postId.value;
    if (!id || this.form.controls.postId.invalid) {
      this.form.controls.postId.markAsTouched();
      return;
    }
    if (!(await this.confirm.confirm({
      titleKey: 'communityModeration.post.title',
      messageKey: 'communityModeration.post.message',
      confirmKey: 'communityModeration.post.confirm',
      cancelKey: 'common.actions.cancel',
    }))) return;
    this.busy.set(true);
    const res = await this.api.softDeletePost(id);
    this.busy.set(false);
    if (res.ok) {
      this.toast.success('communityModeration.post.toast');
      this.form.controls.postId.reset();
    } else this.toast.error(`errors.${res.error.kind}`);
  }

  async deleteReply(): Promise<void> {
    const id = this.form.controls.replyId.value;
    if (!id || this.form.controls.replyId.invalid) {
      this.form.controls.replyId.markAsTouched();
      return;
    }
    if (!(await this.confirm.confirm({
      titleKey: 'communityModeration.reply.title',
      messageKey: 'communityModeration.reply.message',
      confirmKey: 'communityModeration.reply.confirm',
      cancelKey: 'common.actions.cancel',
    }))) return;
    this.busy.set(true);
    const res = await this.api.softDeleteReply(id);
    this.busy.set(false);
    if (res.ok) {
      this.toast.success('communityModeration.reply.toast');
      this.form.controls.replyId.reset();
    } else this.toast.error(`errors.${res.error.kind}`);
  }
}
