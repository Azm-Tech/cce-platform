import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LocaleService } from '@frontend/i18n';
import { ToastService } from '@frontend/ui-kit';
import { TranslateModule } from '@ngx-translate/core';
import { EventsApiService } from './events-api.service';
import type { Event } from './event.types';

@Component({
  selector: 'cce-event-detail',
  standalone: true,
  imports: [
    CommonModule, DatePipe, RouterLink,
    MatButtonModule, MatCardModule, MatIconModule, MatProgressSpinnerModule,
    TranslateModule,
  ],
  templateUrl: './event-detail.page.html',
  styleUrl: './event-detail.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EventDetailPage implements OnInit {
  private readonly api = inject(EventsApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly localeService = inject(LocaleService);
  private readonly toast = inject(ToastService);

  readonly event = signal<Event | null>(null);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly downloading = signal(false);

  readonly locale = this.localeService.locale;

  readonly title = computed(() => {
    const e = this.event();
    if (!e) return '';
    return this.locale() === 'ar' ? e.titleAr : e.titleEn;
  });

  readonly description = computed(() => {
    const e = this.event();
    if (!e) return '';
    return this.locale() === 'ar' ? e.descriptionAr : e.descriptionEn;
  });

  readonly location = computed(() => {
    const e = this.event();
    if (!e) return null;
    if (e.onlineMeetingUrl) return e.onlineMeetingUrl;
    return this.locale() === 'ar' ? e.locationAr : e.locationEn;
  });

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.errorKind.set('not-found');
      return;
    }
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.getEvent(id);
    this.loading.set(false);
    if (res.ok) this.event.set(res.value);
    else this.errorKind.set(res.error.kind);
  }

  async exportToCalendar(): Promise<void> {
    const e = this.event();
    if (!e) return;
    this.downloading.set(true);
    const res = await this.api.downloadIcs(e.id);
    this.downloading.set(false);
    if (!res.ok) {
      this.toast.error(`errors.${res.error.kind}`);
      return;
    }
    this.saveBlob(res.value, this.filenameFor(e));
    this.toast.success('events.export.toast');
  }

  private filenameFor(e: Event): string {
    const safeTitle = e.titleEn.replace(/[^a-zA-Z0-9_-]+/g, '-').replace(/^-+|-+$/g, '') || 'event';
    return `${safeTitle}.ics`;
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
