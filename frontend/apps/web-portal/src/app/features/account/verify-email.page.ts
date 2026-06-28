import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslocoModule } from '@jsverse/transloco';
import { AuthApiService } from '../../core/auth/auth-api.service';

type PageState = 'verifying' | 'success' | 'error';

@Component({
  selector: 'cce-verify-email',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatProgressSpinnerModule, TranslocoModule],
  template: `
    <section class="cce-auth">
      <div class="cce-auth__card">
        <div class="cce-auth__brand">
          <span class="cce-auth__brand-glyph" aria-hidden="true"></span>
          <span class="cce-auth__brand-name">CCE</span>
        </div>

        <h1 class="cce-auth__title">{{ 'account.verifyEmail.title' | transloco }}</h1>

        @if (state() === 'verifying') {
          <p class="cce-auth__subtitle">{{ 'account.verifyEmail.verifying' | transloco }}</p>
          <mat-spinner diameter="40" class="cce-auth__spinner" />
        }

        @if (state() === 'success') {
          <p class="cce-auth__subtitle">{{ 'account.verifyEmail.successBody' | transloco }}</p>
          <a routerLink="/login" mat-flat-button color="primary" class="cce-auth__submit">
            {{ 'account.verifyEmail.signInLink' | transloco }}
          </a>
        }

        @if (state() === 'error') {
          <p class="cce-auth__subtitle cce-auth__subtitle--error">
            {{ 'account.verifyEmail.errorBody' | transloco }}
          </p>
          <a routerLink="/login" mat-stroked-button color="primary" class="cce-auth__submit">
            {{ 'account.verifyEmail.backToLogin' | transloco }}
          </a>
        }
      </div>
    </section>
  `,
  styleUrl: './forgot-password.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VerifyEmailPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly authApi = inject(AuthApiService);

  readonly state = signal<PageState>('verifying');

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!token) {
      this.state.set('error');
      return;
    }
    this.authApi.verifyEmail(token).subscribe({
      next: () => this.state.set('success'),
      error: () => this.state.set('error'),
    });
  }
}
