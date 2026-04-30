import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { TranslateModule } from '@ngx-translate/core';
import { KnowledgeApiService } from './knowledge-api.service';
import type { Resource } from './knowledge.types';

@Component({
  selector: 'cce-resource-detail',
  standalone: true,
  imports: [
    CommonModule, DatePipe, RouterLink,
    MatButtonModule, MatCardModule, MatIconModule, MatProgressSpinnerModule,
    TranslateModule,
  ],
  templateUrl: './resource-detail.page.html',
  styleUrl: './resource-detail.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResourceDetailPage implements OnInit {
  private readonly api = inject(KnowledgeApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly localeService = inject(LocaleService);
  private readonly toast = inject(ToastService);

  readonly resource = signal<Resource | null>(null);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly downloading = signal(false);

  readonly locale = this.localeService.locale;

  readonly title = computed(() => {
    const r = this.resource();
    if (!r) return '';
    return this.locale() === 'ar' ? r.titleAr : r.titleEn;
  });

  readonly description = computed(() => {
    const r = this.resource();
    if (!r) return '';
    return this.locale() === 'ar' ? r.descriptionAr : r.descriptionEn;
  });

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.errorKind.set('not-found');
      return;
    }
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.getResource(id);
    this.loading.set(false);
    if (res.ok) this.resource.set(res.value);
    else this.errorKind.set(res.error.kind);
  }

  async download(): Promise<void> {
    const r = this.resource();
    if (!r) return;
    this.downloading.set(true);
    const res = await this.api.download(r.id);
    this.downloading.set(false);
    if (!res.ok) {
      this.toast.error(`errors.${res.error.kind}`);
      return;
    }
    this.saveBlob(res.value, this.filenameFor(r));
    this.toast.success('resources.download.toast');
  }

  iconFor(type: Resource['resourceType']): string {
    switch (type) {
      case 'Pdf': return 'picture_as_pdf';
      case 'Video': return 'play_circle';
      case 'Image': return 'image';
      case 'Link': return 'link';
      case 'Document': return 'description';
    }
  }

  private filenameFor(r: Resource): string {
    const safeTitle = r.titleEn.replace(/[^a-zA-Z0-9_-]+/g, '-').replace(/^-+|-+$/g, '') || 'resource';
    const ext = r.resourceType === 'Pdf' ? '.pdf'
      : r.resourceType === 'Video' ? '.mp4'
      : r.resourceType === 'Image' ? '.jpg'
      : r.resourceType === 'Document' ? '.docx'
      : '';
    return `${safeTitle}${ext}`;
  }

  private saveBlob(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }
}
