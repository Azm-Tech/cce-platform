import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoModule } from '@jsverse/transloco';
import { AuthService } from '../auth/auth.service';

@Component({
  selector: 'cce-auth-toolbar',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, TranslocoModule],
  template: `
    @if (isAuthenticated()) {
      <span class="cce-auth-toolbar__label">{{ userLabel() }}</span>
      <button mat-icon-button shellHeaderEnd (click)="signOut()" [attr.aria-label]="'account.logout.button' | transloco">
        <mat-icon>logout</mat-icon>
      </button>
    }
  `,
  styles: [`.cce-auth-toolbar__label { font-size: 0.875rem; opacity: 0.8; margin-inline-end: 0.25rem; }`],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthToolbarComponent {
  private readonly auth = inject(AuthService);

  readonly isAuthenticated = this.auth.isAuthenticated;
  readonly userLabel = computed(() => {
    const u = this.auth.currentUser();
    return u ? `${u.firstName} ${u.lastName}`.trim() : '';
  });

  async signOut(): Promise<void> {
    await this.auth.signOut();
  }
}
