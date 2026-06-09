
import { DatePipe, DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { MediaApiService } from '../../core/media/media-api.service';
import { contentRequestStatusKey, ContentType, type CountryContentRequest } from './country.types';

@Component({
  selector: 'cce-request-detail-dialog',
  standalone: true,
  imports: [DatePipe, MatDialogModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, TranslocoModule],
  templateUrl: './request-detail.dialog.html',
  styleUrl: './request-detail.dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestDetailDialogComponent {
  private readonly sanitizer = inject(DomSanitizer);
  private readonly media = inject(MediaApiService);
  private readonly toast = inject(ToastService);
  private readonly document = inject(DOCUMENT);
  readonly request = inject<CountryContentRequest>(MAT_DIALOG_DATA);
  readonly locale = inject(LocaleService).locale;
  readonly statusKey = contentRequestStatusKey;
  readonly ContentType = ContentType;
  readonly downloading = signal(false);

  safe(html: string | null) {
    return html ? this.sanitizer.bypassSecurityTrustHtml(html) : '';
  }

  async downloadAsset(): Promise<void> {
    const assetId = this.request.proposedAssetFileId;
    if (!assetId) return;
    this.downloading.set(true);
    const res = await this.media.getAsset(assetId);
    this.downloading.set(false);
    if (!res.ok) {
      this.toast.error('errors.ERR002');
      return;
    }
    const { url } = res.value;
    // Resolve relative URLs against the current origin
    const absolute = url.startsWith('http') ? url : new URL(url, this.document.location.origin).toString();
    const a = this.document.createElement('a');
    a.href = absolute;
    a.target = '_blank';
    a.rel = 'noopener noreferrer';
    // Let the browser determine the filename from the URL / Content-Disposition
    this.document.body.appendChild(a);
    a.click();
    this.document.body.removeChild(a);
  }
}
