import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  signal,
} from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoModule } from '@jsverse/transloco';
import { categorize, extensionOf, type MediaCategory } from './lib/attachment-rules';
import { MediaLightboxComponent, type LightboxImage } from './media-lightbox.component';
import type { PostMedia } from './community.types';

interface ResolvedAttachment {
  id: string;
  url: string;
  name: string;
  sizeBytes: number | null;
  category: MediaCategory;
  ext: string;
}

/**
 * Renders a post's resolved `media[]` (public URLs + metadata supplied by the
 * post detail endpoint) as an image grid (with lightbox), a video player, and
 * downloadable file cards. Category is derived from the file extension /
 * mimeType; the server `kind` ("document") is used as a fallback.
 */
@Component({
  selector: 'cce-post-media-gallery',
  standalone: true,
  imports: [MatIconModule, TranslocoModule, MediaLightboxComponent],
  templateUrl: './post-media-gallery.component.html',
  styleUrl: './post-media-gallery.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostMediaGalleryComponent {
  readonly media = input<PostMedia[]>([]);
  readonly locale = input<'ar' | 'en'>('en');

  private readonly resolved = computed<ResolvedAttachment[]>(() => {
    const items = [...(this.media() ?? [])].sort(
      (a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0),
    );
    const out: ResolvedAttachment[] = [];
    for (const m of items) {
      if (!m.url) continue;
      const name = m.originalFileName ?? m.assetFileId;
      const category =
        categorize(name, m.mimeType ?? undefined) ??
        (m.kind === 'document' ? 'file' : null);
      if (!category) continue;
      out.push({
        id: m.assetFileId,
        url: m.url,
        name,
        sizeBytes: m.sizeBytes ?? null,
        category,
        ext: extensionOf(name),
      });
    }
    return out;
  });

  readonly images = computed(() => this.resolved().filter((a) => a.category === 'image'));
  readonly video = computed(() => this.resolved().find((a) => a.category === 'video') ?? null);
  readonly files = computed(() => this.resolved().filter((a) => a.category === 'file'));

  /** Image set passed to the lightbox. */
  readonly lightboxImages = computed<LightboxImage[]>(() =>
    this.images().map((a) => ({ url: a.url, name: a.name })),
  );
  /** Open lightbox index, or null when closed. */
  readonly lightboxIndex = signal<number | null>(null);

  openLightbox(image: ResolvedAttachment): void {
    const idx = this.images().findIndex((a) => a.id === image.id);
    if (idx >= 0) this.lightboxIndex.set(idx);
  }

  closeLightbox(): void {
    this.lightboxIndex.set(null);
  }

  setLightboxIndex(i: number): void {
    this.lightboxIndex.set(i);
  }

  fileIcon(ext: string): string {
    if (ext === 'pdf') return 'file-text';
    if (ext === 'ppt' || ext === 'pptx') return 'monitor';
    if (ext === 'xls' || ext === 'xlsx') return 'table';
    return 'file-text';
  }

  formatFileSize(bytes: number | null): string {
    if (bytes == null || bytes === 0) return '';
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }
}
