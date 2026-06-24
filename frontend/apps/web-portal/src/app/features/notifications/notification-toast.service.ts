import { Injectable, inject } from '@angular/core';
import { MatSnackBar, MatSnackBarRef } from '@angular/material/snack-bar';
import { TranslocoService } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { NotificationToastComponent, type NotificationToastData } from './notification-toast.component';

const TOAST_DURATION_MS = 10000;

/** Minimal localized-content source a toast renders from — satisfied by both a
 *  `UserNotification` and a realtime `ReceiveNotification` payload. */
export interface NotificationToastSource {
  renderedSubjectAr: string | null;
  renderedSubjectEn: string | null;
  renderedBody: string | null;
}

/**
 * Opens the branded live-notification toast. Callers pass the notification
 * content (the realtime `ReceiveNotification` payload carries it directly), or
 * call `showGeneric` when none is available.
 */
@Injectable({ providedIn: 'root' })
export class NotificationToastService {
  private readonly snack = inject(MatSnackBar);
  private readonly translate = inject(TranslocoService);
  private readonly localeService = inject(LocaleService);

  /** The currently-visible toast, if any — guards against stacking (which flashes). */
  private activeRef: MatSnackBarRef<NotificationToastComponent> | null = null;

  /** Show a toast for a concrete notification. `onView` runs if the user taps "View". */
  show(notification: NotificationToastSource, onView: () => void): void {
    this.open(this.buildData(notification), onView);
  }

  /** Fallback when no notification detail is available — generic headline only. */
  showGeneric(onView: () => void): void {
    this.open(this.genericData(), onView);
  }

  /** Toast for a live new post (community/topic feed event). `title` from the payload. */
  showNewPost(title: string | null, onView: () => void): void {
    this.open(
      {
        ...this.commonLabels(),
        icon: 'article',
        title: title?.trim() || this.translate.translate('notifications.newPostTitle'),
        body: this.translate.translate('notifications.newPostBody'),
      },
      onView,
    );
  }

  private open(data: NotificationToastData, onView: () => void): void {
    // A toast is already showing — don't open a second (replacing it mid-animation
    // causes the appear/disappear/appear flash). Coalesce: skip the new one.
    if (this.activeRef) return;

    const ref = this.snack.openFromComponent(NotificationToastComponent, {
      data,
      duration: data.durationMs,
      // Explicit side per locale: Arabic (RTL) → top-left, English (LTR) → top-right.
      horizontalPosition: this.localeService.locale() === 'ar' ? 'left' : 'right',
      verticalPosition: 'top',
      panelClass: 'cce-notif-toast',
    });
    this.activeRef = ref;
    ref.afterDismissed().subscribe(() => {
      if (this.activeRef === ref) this.activeRef = null;
    });
    ref.onAction().subscribe(() => onView());
  }

  private buildData(n: NotificationToastSource): NotificationToastData {
    const subject = (this.localeService.locale() === 'ar' ? n.renderedSubjectAr : n.renderedSubjectEn)?.trim();
    return {
      ...this.commonLabels(),
      title: subject || this.translate.translate('notifications.toastNew'),
      body: this.snippet(n.renderedBody),
    };
  }

  private genericData(): NotificationToastData {
    return {
      ...this.commonLabels(),
      title: this.translate.translate('notifications.toastNew'),
      body: '',
    };
  }

  private commonLabels(): Omit<NotificationToastData, 'title' | 'body'> {
    return {
      time: this.translate.translate('notifications.toastNow'),
      actionLabel: this.translate.translate('notifications.toastView'),
      dismissLabel: this.translate.translate('notifications.toastDismiss'),
      icon: 'notifications',
      durationMs: TOAST_DURATION_MS,
    };
  }

  /** Strip markup/whitespace from the rendered body and clamp to a short snippet. */
  private snippet(body: string | null): string {
    if (!body) return '';
    const text = body.replace(/<[^>]*>/g, ' ').replace(/\s+/g, ' ').trim();
    return text.length > 120 ? `${text.slice(0, 117)}…` : text;
  }
}
