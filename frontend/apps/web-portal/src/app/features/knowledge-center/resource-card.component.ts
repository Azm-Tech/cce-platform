import {
  ChangeDetectionStrategy, Component, computed, inject, input, signal,
} from '@angular/core';
import { CommonModule, DatePipe, DOCUMENT } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslocoModule } from '@jsverse/transloco';
import { ToastService } from '@frontend/ui-kit';
import { KnowledgeApiService } from './knowledge-api.service';
import type { ResourceListItem, ResourceType } from './knowledge.types';

@Component({
  selector: 'cce-resource-card',
  standalone: true,
  imports: [
    CommonModule, DatePipe, RouterLink,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule, TranslocoModule,
  ],
  template: `
    <article class="cce-resource-card" [attr.data-type]="resource().resourceType">

      <!-- ── Media area ── -->
      <a class="cce-resource-card__media"
         [routerLink]="['/knowledge-center', resource().id]"
         [attr.aria-label]="title()">
        <div class="cce-resource-card__media-icon">
          <mat-icon aria-hidden="true">{{ iconFor(resource().resourceType) }}</mat-icon>
        </div>
        <span class="cce-resource-card__badge">
          <mat-icon aria-hidden="true">{{ iconFor(resource().resourceType) }}</mat-icon>
          {{ ('resources.type.' + resource().resourceType) | transloco }}
        </span>
      </a>

      <!-- ── Body ── -->
      <div class="cce-resource-card__body">
        <a class="cce-resource-card__title-link"
           [routerLink]="['/knowledge-center', resource().id]">
          <h3 class="cce-resource-card__title">{{ title() }}</h3>
        </a>
        @if (categoryName()) {
          <p class="cce-resource-card__category">{{ categoryName() }}</p>
        }
      </div>

      <!-- ── Meta ── -->
      <div class="cce-resource-card__meta">
        @if (resource().publishedOn) {
          <span class="cce-resource-card__meta-item">
            <mat-icon aria-hidden="true">calendar_today</mat-icon>
            {{ resource().publishedOn | date:'mediumDate' }}
          </span>
        }
        @if (resource().assetFileName) {
          <span class="cce-resource-card__meta-item">
            <mat-icon aria-hidden="true">attach_file</mat-icon>
            {{ resource().assetFileName }}
          </span>
        }
      </div>

      <!-- ── Actions ── -->
      <footer class="cce-resource-card__footer">
        <button mat-stroked-button
          class="cce-resource-card__download-btn"
          (click)="onDownload($event)"
          [disabled]="downloading()">
          @if (downloading()) {
            <mat-spinner diameter="16" />
          } @else {
            <mat-icon>download</mat-icon>
          }
          {{ 'resources.download.openButton' | transloco }}
        </button>
        <button mat-icon-button
          class="cce-resource-card__share-btn"
          [attr.aria-label]="'share.button' | transloco"
          (click)="onShare($event)">
          <mat-icon>share</mat-icon>
        </button>
      </footer>
    </article>
  `,
  styleUrl: './resource-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResourceCardComponent {
  private readonly document = inject(DOCUMENT);
  private readonly router = inject(Router);
  private readonly api = inject(KnowledgeApiService);
  private readonly toast = inject(ToastService);

  readonly resource = input.required<ResourceListItem>();
  readonly locale = input<'ar' | 'en'>('en');

  readonly downloading = signal(false);

  readonly title = computed(() =>
    this.locale() === 'ar' ? this.resource().titleAr : this.resource().titleEn,
  );

  readonly categoryName = computed<string | null>(() => {
    const r = this.resource();
    return (this.locale() === 'ar' ? r.categoryNameAr : r.categoryNameEn) ?? null;
  });

  iconFor(type: ResourceType): string {
    switch (type) {
      case 'Paper':          return 'article';
      case 'Article':        return 'article';
      case 'Study':          return 'description';
      case 'Presentation':   return 'slideshow';
      case 'ScientificPaper':return 'science';
      case 'Report':         return 'assessment';
      case 'Book':           return 'menu_book';
      case 'Research':       return 'biotech';
      case 'CceGuide':       return 'eco';
      case 'Media':          return 'play_circle';
    }
  }

  onDownload(event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();
    void this.doDownload();
  }

  onShare(event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();
    void this.doShare();
  }

  private async doDownload(): Promise<void> {
    const r = this.resource();
    this.downloading.set(true);
    try {
      const res = await this.api.download(r.id);
      this.downloading.set(false);
      if (res.ok) {
        this.saveBlob(res.value, this.filenameFor(r));
        this.toast.success('confirmations.CON001');
      } else {
        void this.router.navigate(['/knowledge-center', r.id]);
      }
    } catch {
      this.downloading.set(false);
      this.toast.error('errors.ERR002');
    }
  }

  private async doShare(): Promise<void> {
    const url = `${this.document.location.origin}/knowledge-center/${this.resource().id}`;
    const nav = this.document.defaultView?.navigator as Navigator | undefined;
    try {
      if (nav?.share) {
        await nav.share({ title: this.title(), url });
        this.toast.success('confirmations.CON002');
      } else {
        await nav?.clipboard?.writeText(url);
        this.toast.success('confirmations.CON002');
      }
    } catch (err) {
      if (err instanceof Error && err.name !== 'AbortError') {
        this.toast.error('errors.ERR004');
      }
    }
  }

  private filenameFor(r: ResourceListItem): string {
    const safeTitle = r.titleEn.replace(/[^a-zA-Z0-9_-]+/g, '-').replace(/^-+|-+$/g, '') || 'resource';
    const ext = r.resourceType === 'Media' ? '.mp4'
      : r.resourceType === 'Presentation' ? '.pptx'
      : '.pdf';
    return `${safeTitle}${ext}`;
  }

  private saveBlob(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const a = this.document.createElement('a');
    a.href = url;
    a.download = filename;
    this.document.body.appendChild(a);
    a.click();
    this.document.body.removeChild(a);
    URL.revokeObjectURL(url);
  }
}
