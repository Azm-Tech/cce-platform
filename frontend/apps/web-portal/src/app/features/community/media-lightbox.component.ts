import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  OnDestroy,
  OnInit,
  computed,
  inject,
  input,
  output,
} from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoModule } from '@jsverse/transloco';

export interface LightboxImage {
  url: string;
  name: string;
}

/**
 * Full-screen image lightbox — a plain fixed overlay (NOT a MatDialog) so it
 * stays OnPush-friendly and inside the page's Transloco scope. Esc closes,
 * arrows navigate, backdrop click closes.
 */
@Component({
  selector: 'cce-media-lightbox',
  standalone: true,
  imports: [MatIconModule, TranslocoModule],
  templateUrl: './media-lightbox.component.html',
  styleUrl: './media-lightbox.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MediaLightboxComponent implements OnInit, OnDestroy {
  private readonly doc = inject(DOCUMENT);

  readonly images = input<LightboxImage[]>([]);
  readonly index = input<number>(0);

  readonly closed = output<void>();
  readonly indexChange = output<number>();

  readonly current = computed<LightboxImage | null>(() => this.images()[this.index()] ?? null);
  readonly hasMultiple = computed(() => this.images().length > 1);
  readonly counter = computed(() => `${this.index() + 1} / ${this.images().length}`);

  ngOnInit(): void {
    this.doc.body.classList.add('cce-lightbox-open');
  }

  ngOnDestroy(): void {
    this.doc.body.classList.remove('cce-lightbox-open');
  }

  onClose(): void {
    this.closed.emit();
  }

  onBackdrop(event: MouseEvent): void {
    if (event.target === event.currentTarget) this.closed.emit();
  }

  prev(): void {
    const n = this.images().length;
    if (n === 0) return;
    this.indexChange.emit((this.index() - 1 + n) % n);
  }

  next(): void {
    const n = this.images().length;
    if (n === 0) return;
    this.indexChange.emit((this.index() + 1) % n);
  }

  @HostListener('document:keydown.escape')
  onEsc(): void {
    this.closed.emit();
  }

  @HostListener('document:keydown.arrowleft')
  onLeft(): void {
    this.next(); // RTL-aware: left advances visually-leading direction
  }

  @HostListener('document:keydown.arrowright')
  onRight(): void {
    this.prev();
  }
}
