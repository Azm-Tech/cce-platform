import { DatePipe, DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { DomSanitizer, type SafeHtml } from '@angular/platform-browser';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslocoModule } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { ApproveCountryRequestDialogComponent } from './approve-country-request.dialog';
import { RejectCountryRequestDialogComponent } from './reject-country-request.dialog';
import { ContentApiService } from './content-api.service';
import {
  AdminContentRequestStatus,
  AdminContentType,
  adminContentRequestStatusKey,
  RESOURCE_TYPE_FROM_VALUE,
  type AdminCountryContentRequest,
} from './content.types';

@Component({
  selector: 'cce-country-request-detail',
  standalone: true,
  imports: [
    DatePipe,
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    TranslocoModule,
  ],
  templateUrl: './country-request-detail.page.html',
  styleUrl: './country-request-detail.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CountryRequestDetailPage implements OnInit {
  private readonly api = inject(ContentApiService);
  private readonly dialog = inject(MatDialog);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly document = inject(DOCUMENT);
  private readonly sanitizer = inject(DomSanitizer);
  readonly locale = inject(LocaleService).locale;

  safe(html: string | null | undefined): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(html ?? '');
  }

  readonly AdminContentType = AdminContentType;
  readonly AdminContentRequestStatus = AdminContentRequestStatus;
  readonly statusKey = adminContentRequestStatusKey;
  readonly resourceTypeLabel = (n: number | null) =>
    n != null ? RESOURCE_TYPE_FROM_VALUE[n] ?? String(n) : '—';

  readonly request = signal<AdminCountryContentRequest | null>(null);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly downloading = signal(false);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    void this.load(id);
  }

  async load(id: string): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.getCountryRequest(id);
    this.loading.set(false);
    if (res.ok) this.request.set(res.value);
    else this.errorKind.set(res.error.kind);
  }

  async approve(): Promise<void> {
    const r = this.request();
    if (!r) return;
    const ref = this.dialog.open(ApproveCountryRequestDialogComponent, {
      data: { requestId: r.id, titleAr: r.proposedTitleAr, titleEn: r.proposedTitleEn },
      width: '500px',
    });
    const updated = await firstValueFrom(ref.afterClosed());
    if (updated) {
      this.toast.success('countryRequest.approve.toast');
      this.request.set(updated);
    }
  }

  async reject(): Promise<void> {
    const r = this.request();
    if (!r) return;
    const ref = this.dialog.open(RejectCountryRequestDialogComponent, {
      data: { requestId: r.id },
      width: '500px',
    });
    const updated = await firstValueFrom(ref.afterClosed());
    if (updated) {
      this.toast.success('countryRequest.reject.toast');
      this.request.set(updated);
    }
  }

  async downloadFile(assetId: string): Promise<void> {
    this.downloading.set(true);
    const res = await this.api.downloadAsset(assetId);
    this.downloading.set(false);
    if (!res.ok) {
      this.toast.error('errors.' + res.error.kind);
      return;
    }
    const url = URL.createObjectURL(res.value);
    const a = this.document.createElement('a');
    a.href = url;
    a.download = assetId;
    a.click();
    URL.revokeObjectURL(url);
  }
}
