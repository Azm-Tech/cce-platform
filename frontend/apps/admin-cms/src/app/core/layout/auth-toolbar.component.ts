import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { TranslocoModule } from '@jsverse/transloco';
import { AuthService } from '../auth/auth.service';

@Component({
  selector: 'cce-auth-toolbar',
  standalone: true,
  imports: [MatButtonModule, MatDividerModule, MatIconModule, MatMenuModule, TranslocoModule],
  template: `
    @if (isAuthenticated()) {
      <button mat-icon-button shellHeaderEnd [matMenuTriggerFor]="profileMenu"
              [attr.aria-label]="'account.profile' | transloco">
        <mat-icon>account_circle</mat-icon>
      </button>

      <mat-menu #profileMenu="matMenu" class="cce-profile-menu">
        <div class="cce-profile-menu__header" (click)="$event.stopPropagation()">
          <span class="cce-profile-menu__name">{{ fullName() }}</span>
          <span class="cce-profile-menu__email">{{ email() }}</span>
        </div>
        <mat-divider />
        <button mat-menu-item (click)="signOut()">
          <mat-icon>logout</mat-icon>
          {{ 'account.logout.button' | transloco }}
        </button>
      </mat-menu>
    }
  `,
  styles: [`
    .cce-profile-menu__header {
      display: flex;
      flex-direction: column;
      padding: 12px 16px;
      pointer-events: none;
    }
    .cce-profile-menu__name {
      font-weight: 600;
      font-size: 0.9rem;
      line-height: 1.4;
    }
    .cce-profile-menu__email {
      font-size: 0.8rem;
      color: rgba(0,0,0,0.55);
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthToolbarComponent {
  private readonly auth = inject(AuthService);

  readonly isAuthenticated = this.auth.isAuthenticated;

  readonly fullName = computed(() => {
    const u = this.auth.currentUser();
    return u ? `${u.firstName} ${u.lastName}`.trim() : '';
  });

  readonly email = computed(() => this.auth.currentUser()?.emailAddress ?? '');

  async signOut(): Promise<void> {
    await this.auth.signOut();
  }
}
