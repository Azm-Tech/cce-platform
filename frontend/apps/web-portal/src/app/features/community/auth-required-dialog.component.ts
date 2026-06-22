import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';
import { TranslocoModule } from '@jsverse/transloco';

export interface AuthRequiredDialogData {
  /** Optional i18n key for the context line (e.g. "sign in to vote"). */
  messageKey?: string;
}

/**
 * Shown when an anonymous visitor triggers a community write-action
 * (vote, reply, follow). Offers a choice between signing in and
 * registering, preserving the current URL as the post-auth return target.
 */
@Component({
  selector: 'cce-auth-required-dialog',
  standalone: true,
  imports: [MatIconModule, TranslocoModule],
  // MUST be Default — OnPush breaks Transloco inside the MatDialog overlay boundary
  changeDetection: ChangeDetectionStrategy.Default,
  styleUrl: './auth-required-dialog.component.scss',
  template: `
    <div class="ard">

      <div class="ard__header">
        <h2 class="ard__title">{{ 'community.authDialog.title' | transloco }}</h2>
        <button type="button" class="ard__close" (click)="close()" [attr.aria-label]="'community.authDialog.close' | transloco">
          <mat-icon svgIcon="x" aria-hidden="true"></mat-icon>
        </button>
      </div>

      <div class="ard__body">
        <span class="ard__icon" aria-hidden="true">
          <mat-icon svgIcon="log-in" aria-hidden="true"></mat-icon>
        </span>
        <p class="ard__message">{{ messageKey | transloco }}</p>
      </div>

      <div class="ard__actions">
        <button type="button" class="ard__btn ard__btn--primary" (click)="goToLogin()">
          {{ 'community.authDialog.login' | transloco }}
        </button>
        <button type="button" class="ard__btn ard__btn--ghost" (click)="goToRegister()">
          {{ 'community.authDialog.register' | transloco }}
        </button>
      </div>

    </div>
  `,
})
export class AuthRequiredDialogComponent {
  private readonly dialogRef = inject(MatDialogRef<AuthRequiredDialogComponent>);
  private readonly router = inject(Router);
  private readonly data = inject<AuthRequiredDialogData>(MAT_DIALOG_DATA, { optional: true });

  readonly messageKey = this.data?.messageKey ?? 'community.authDialog.message';

  private readonly returnUrl = window.location.pathname + window.location.search;

  goToLogin(): void {
    this.dialogRef.close();
    void this.router.navigate(['/login'], {
      queryParams: this.returnUrl && this.returnUrl !== '/login' ? { returnUrl: this.returnUrl } : undefined,
    });
  }

  goToRegister(): void {
    this.dialogRef.close();
    void this.router.navigate(['/register'], {
      queryParams: this.returnUrl && this.returnUrl !== '/register' ? { returnUrl: this.returnUrl } : undefined,
    });
  }

  close(): void {
    this.dialogRef.close();
  }
}
