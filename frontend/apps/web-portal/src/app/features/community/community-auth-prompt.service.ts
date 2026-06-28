import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { AuthService } from '../../core/auth/auth.service';
import {
  AuthRequiredDialogComponent,
  type AuthRequiredDialogData,
} from './auth-required-dialog.component';

/**
 * Gatekeeper for community write-actions (vote, reply, follow). Call
 * {@link requireAuth} at the start of any handler that needs a signed-in
 * user: it returns `true` when authenticated, otherwise opens the
 * login/register dialog and returns `false` so the caller can bail out.
 */
@Injectable({ providedIn: 'root' })
export class CommunityAuthPromptService {
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);

  /** @param messageKey optional i18n key for the contextual dialog line. */
  requireAuth(messageKey?: string): boolean {
    if (this.auth.isAuthenticated()) return true;
    this.dialog.open<AuthRequiredDialogComponent, AuthRequiredDialogData>(
      AuthRequiredDialogComponent,
      {
        data: { messageKey },
        width: '440px',
        maxWidth: '95vw',
        autoFocus: false,
        panelClass: 'cce-auth-required-dialog',
      },
    );
    return false;
  }
}
