import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoModule } from '@jsverse/transloco';

export interface SharePostDialogData {
  postId: string;
  postTitle: string | null;
}

@Component({
  selector: 'cce-share-post-dialog',
  standalone: true,
  imports: [MatIconModule, TranslocoModule],
  // MUST be Default — OnPush breaks Transloco inside MatDialog overlay boundary
  changeDetection: ChangeDetectionStrategy.Default,
  styleUrl: './share-post-dialog.component.scss',
  template: `
    <div class="spd">

      <div class="spd__header">
        <h2 class="spd__title">{{ 'community.shareDialog.title' | transloco }}</h2>
        <button type="button" class="spd__close" (click)="close()" [attr.aria-label]="'community.shareDialog.close' | transloco">
          <mat-icon svgIcon="x" aria-hidden="true"></mat-icon>
        </button>
      </div>

      <div class="spd__platforms">

        <button type="button" class="spd__platform" (click)="open('twitter')">
          <span class="spd__icon spd__icon--twitter">
            <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
              <path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-4.714-6.231-5.401 6.231H2.748l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z"/>
            </svg>
          </span>
          <span class="spd__platform-name">Twitter</span>
        </button>

        <button type="button" class="spd__platform" (click)="open('linkedin')">
          <span class="spd__icon spd__icon--linkedin">
            <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
              <path d="M20.447 20.452h-3.554v-5.569c0-1.328-.027-3.037-1.852-3.037-1.853 0-2.136 1.445-2.136 2.939v5.667H9.351V9h3.414v1.561h.046c.477-.9 1.637-1.85 3.37-1.85 3.601 0 4.267 2.37 4.267 5.455v6.286zM5.337 7.433a2.062 2.062 0 0 1-2.063-2.065 2.064 2.064 0 1 1 2.063 2.065zm1.782 13.019H3.555V9h3.564v11.452zM22.225 0H1.771C.792 0 0 .774 0 1.729v20.542C0 23.227.792 24 1.771 24h20.451C23.2 24 24 23.227 24 22.271V1.729C24 .774 23.2 0 22.222 0h.003z"/>
            </svg>
          </span>
          <span class="spd__platform-name">LinkedIn</span>
        </button>

        <button type="button" class="spd__platform" (click)="open('whatsapp')">
          <span class="spd__icon spd__icon--whatsapp">
            <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
              <path d="M17.472 14.382c-.297-.149-1.758-.867-2.03-.967-.273-.099-.471-.148-.67.15-.197.297-.767.966-.94 1.164-.173.199-.347.223-.644.075-.297-.15-1.255-.463-2.39-1.475-.883-.788-1.48-1.761-1.653-2.059-.173-.297-.018-.458.13-.606.134-.133.298-.347.446-.52.149-.174.198-.298.298-.497.099-.198.05-.371-.025-.52-.075-.149-.669-1.612-.916-2.207-.242-.579-.487-.5-.669-.51-.173-.008-.371-.01-.57-.01-.198 0-.52.074-.792.372-.272.297-1.04 1.016-1.04 2.479 0 1.462 1.065 2.875 1.213 3.074.149.198 2.096 3.2 5.077 4.487.709.306 1.262.489 1.694.625.712.227 1.36.195 1.871.118.571-.085 1.758-.719 2.006-1.413.248-.694.248-1.289.173-1.413-.074-.124-.272-.198-.57-.347m-5.421 7.403h-.004a9.87 9.87 0 0 1-5.031-1.378l-.361-.214-3.741.982.998-3.648-.235-.374a9.86 9.86 0 0 1-1.51-5.26c.001-5.45 4.436-9.884 9.888-9.884 2.64 0 5.122 1.03 6.988 2.898a9.825 9.825 0 0 1 2.893 6.994c-.003 5.45-4.437 9.884-9.885 9.884m8.413-18.297A11.815 11.815 0 0 0 12.05 0C5.495 0 .16 5.335.157 11.892c0 2.096.547 4.142 1.588 5.945L.057 24l6.305-1.654a11.882 11.882 0 0 0 5.683 1.448h.005c6.554 0 11.89-5.335 11.893-11.893a11.821 11.821 0 0 0-3.48-8.413z"/>
            </svg>
          </span>
          <span class="spd__platform-name">WhatsApp</span>
        </button>

        <button type="button" class="spd__platform" (click)="open('email')">
          <span class="spd__icon spd__icon--email">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
              <rect width="20" height="16" x="2" y="4" rx="2"/>
              <path d="m22 7-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7"/>
            </svg>
          </span>
          <span class="spd__platform-name">Email</span>
        </button>

      </div>

      <div class="spd__copy-row">
        <span class="spd__url" dir="ltr">{{ postUrl }}</span>
        <button type="button" class="spd__copy-btn" (click)="copyLink()">
          @if (copied()) {
            <mat-icon svgIcon="check" aria-hidden="true"></mat-icon>
            {{ 'community.shareDialog.copied' | transloco }}
          } @else {
            <mat-icon svgIcon="copy" aria-hidden="true"></mat-icon>
            {{ 'community.shareDialog.copyLink' | transloco }}
          }
        </button>
      </div>

    </div>
  `,
})
export class SharePostDialogComponent {
  private readonly dialogRef = inject(MatDialogRef<SharePostDialogComponent>);
  readonly data = inject<SharePostDialogData>(MAT_DIALOG_DATA);

  readonly postUrl = `${window.location.origin}/community/posts/${this.data.postId}`;
  readonly copied = signal(false);

  open(platform: 'twitter' | 'linkedin' | 'whatsapp' | 'email'): void {
    const url = encodeURIComponent(this.postUrl);
    const text = encodeURIComponent(this.data.postTitle ?? '');
    const links: Record<string, string> = {
      twitter:  `https://twitter.com/intent/tweet?url=${url}&text=${text}`,
      linkedin: `https://www.linkedin.com/sharing/share-offsite/?url=${url}`,
      whatsapp: `https://wa.me/?text=${text}%20${url}`,
      email:    `mailto:?subject=${text}&body=${url}`,
    };
    if (platform === 'email') {
      window.location.href = links['email'];
    } else {
      window.open(links[platform], '_blank', 'noopener,noreferrer');
    }
  }

  async copyLink(): Promise<void> {
    try {
      await navigator.clipboard.writeText(this.postUrl);
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    } catch { /* clipboard unavailable */ }
  }

  close(): void {
    this.dialogRef.close();
  }
}
