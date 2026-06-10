import { DatePipe } from '@angular/common';
import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslocoModule } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';
import { ToastService } from '@frontend/ui-kit';
import { ApproveExpertDialogComponent, type ApproveExpertDialogData } from './approve-expert.dialog';
import { ExpertApiService } from './expert-api.service';
import type { ExpertRequest } from './expert.types';
import { RejectExpertDialogComponent, type RejectExpertDialogData } from './reject-expert.dialog';
import { TaxonomyApiService } from '../taxonomies/taxonomy-api.service';
import type { Topic } from '../taxonomies/taxonomy.types';

@Component({
  selector: 'cce-expert-request-detail',
  standalone: true,
  imports: [
    DatePipe,
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    TranslocoModule,
  ],
  templateUrl: './expert-request-detail.page.html',
  styleUrl: './expert-request-detail.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExpertRequestDetailPage implements OnInit {
  private readonly api = inject(ExpertApiService);
  private readonly taxonomy = inject(TaxonomyApiService);
  private readonly dialog = inject(MatDialog);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly document = inject(DOCUMENT);

  readonly request = signal<ExpertRequest | null>(null);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly downloading = signal(false);
  private readonly topicsMap = signal<Map<string, Topic>>(new Map());

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    void this.loadTopics();
    void this.load(id);
  }

  private async loadTopics(): Promise<void> {
    const res = await this.taxonomy.listTopics({ pageSize: 200 });
    if (res.ok) {
      this.topicsMap.set(new Map(res.value.items.map(t => [t.id, t])));
    }
  }

  tagLabel(id: string): string {
    const t = this.topicsMap().get(id);
    return t ? (t.nameAr || t.nameEn || id) : id;
  }

  async load(id: string): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.getRequest(id);
    this.loading.set(false);
    if (res.ok) this.request.set(res.value);
    else this.errorKind.set(res.error.kind);
  }

  async downloadCv(): Promise<void> {
    const r = this.request();
    if (!r) return;
    if (r.cvUrl) {
      this.document.defaultView?.open(r.cvUrl, '_blank', 'noopener');
      return;
    }
    if (!r.cvAssetFileId) return;
    this.downloading.set(true);
    const res = await this.api.downloadCvAsset(r.cvAssetFileId);
    this.downloading.set(false);
    if (!res.ok) {
      this.toast.error('errors.' + res.error.kind);
      return;
    }
    const url = URL.createObjectURL(res.value);
    const a = this.document.createElement('a');
    a.href = url;
    a.download = r.cvAssetFileId;
    a.click();
    URL.revokeObjectURL(url);
  }

  async approve(): Promise<void> {
    const r = this.request();
    if (!r) return;
    const data: ApproveExpertDialogData = {
      requestId: r.id,
      requesterName: r.requestedByUserName,
      bioAr: r.requestedBioAr,
      bioEn: r.requestedBioEn,
      requestedTags: r.requestedTags,
      cvUrl: r.cvUrl,
    };
    const ref = this.dialog.open(ApproveExpertDialogComponent, { data, width: '560px' });
    const updated = await firstValueFrom(ref.afterClosed());
    if (updated) {
      this.toast.success('experts.approve.toast');
      this.request.set(updated);
    }
  }

  async reject(): Promise<void> {
    const r = this.request();
    if (!r) return;
    const data: RejectExpertDialogData = {
      requestId: r.id,
      requesterName: r.requestedByUserName,
      bioAr: r.requestedBioAr,
      bioEn: r.requestedBioEn,
      requestedTags: r.requestedTags,
      cvUrl: r.cvUrl,
    };
    const ref = this.dialog.open(RejectExpertDialogComponent, { data, width: '560px' });
    const updated = await firstValueFrom(ref.afterClosed());
    if (updated) {
      this.toast.success('experts.reject.toast');
      this.request.set(updated);
    }
  }
}
