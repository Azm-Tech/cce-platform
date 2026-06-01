import { DatePipe } from '@angular/common';
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
  private readonly dialog = inject(MatDialog);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);

  readonly request = signal<ExpertRequest | null>(null);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    void this.load(id);
  }

  async load(id: string): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.getRequest(id);
    this.loading.set(false);
    if (res.ok) this.request.set(res.value);
    else this.errorKind.set(res.error.kind);
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
