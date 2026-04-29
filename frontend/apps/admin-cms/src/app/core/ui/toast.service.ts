import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TranslateService } from '@ngx-translate/core';

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly snack = inject(MatSnackBar);
  private readonly translate = inject(TranslateService);

  success(messageKey: string, params?: Record<string, unknown>): void {
    this.show(messageKey, params, 'cce-toast-success');
  }

  error(messageKey: string, params?: Record<string, unknown>): void {
    this.show(messageKey, params, 'cce-toast-error');
  }

  private show(key: string, params: Record<string, unknown> | undefined, panelClass: string): void {
    const text = this.translate.instant(key, params);
    this.snack.open(text, undefined, { duration: 4000, panelClass });
  }
}
