import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import { ToastService } from '@frontend/ui-kit';
import { AuthService } from '../../core/auth/auth.service';
import { CommunityApiService } from './community-api.service';
import { SignInCtaComponent } from './sign-in-cta.component';

/**
 * 1-5 star rating widget for a post. Anonymous users see a SignInCta
 * placeholder instead of the widget. Submission is fire-and-forget:
 * a toast confirms; no rollback on error (backend is idempotent for
 * re-rates by the same user).
 */
@Component({
  selector: 'cce-rate-post-control',
  standalone: true,
  imports: [CommonModule, MatIconModule, TranslateModule, SignInCtaComponent],
  templateUrl: './rate-post-control.component.html',
  styleUrl: './rate-post-control.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RatePostControlComponent {
  private readonly api = inject(CommunityApiService);
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);

  readonly postId = input.required<string>();
  /** Optional: when known, pre-highlights the user's previous rating. */
  readonly currentUserRating = input<number | null>(null);

  readonly stars = [1, 2, 3, 4, 5] as const;
  readonly localRating = signal<number | null>(null);
  readonly submitting = signal(false);

  readonly isAuthenticated = this.auth.isAuthenticated;
  readonly displayedRating = computed(
    () => this.localRating() ?? this.currentUserRating() ?? 0,
  );

  isStarFilled(value: number): boolean {
    return value <= this.displayedRating();
  }

  async setRating(stars: number): Promise<void> {
    if (this.submitting()) return;
    this.submitting.set(true);
    const res = await this.api.ratePost(this.postId(), stars);
    this.submitting.set(false);
    if (res.ok) {
      this.localRating.set(stars);
      this.toast.success('community.rate.toast');
    } else {
      this.toast.error('errors.' + res.error.kind);
    }
  }

  /** Keyboard-accessible: Enter / Space toggles. */
  async onKey(value: number, ev: KeyboardEvent): Promise<void> {
    if (ev.key === 'Enter' || ev.key === ' ') {
      ev.preventDefault();
      await this.setRating(value);
    }
  }

  starLabel(value: number): string {
    return `${value} of 5 stars`;
  }
}
