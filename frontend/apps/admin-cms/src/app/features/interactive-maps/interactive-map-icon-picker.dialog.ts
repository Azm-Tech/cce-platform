import { ChangeDetectionStrategy, Component, Inject, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { TranslocoModule } from '@jsverse/transloco';
import { iconDataUri, isCustomIconUrl, MAP_ICON_KEYS } from '@frontend/ui-kit';
import { AssetUploadComponent } from '../content/asset-upload.component';
import type { AssetFile } from '../content/content.types';

export interface IconPickerData {
  /** The node's current iconKey (registry key or uploaded URL), for highlight. */
  current: string | null;
}

type Segment = 'library' | 'upload';

/**
 * Icon picker for a map node. Two modes:
 *  - Library: pick from the built-in line-icon set (preview + name, searchable).
 *  - Upload: upload a custom SVG/PNG (via the shared asset uploader); the
 *    returned URL becomes the icon.
 *
 * Closes with the chosen `iconKey` (string), or `undefined` on cancel
 * (caller leaves the value unchanged). Clearing is handled by the caller.
 */
@Component({
  selector: 'cce-interactive-map-icon-picker',
  standalone: true,
  imports: [
    FormsModule,
    MatButtonModule,
    MatDialogModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    TranslocoModule,
    AssetUploadComponent,
  ],
  templateUrl: './interactive-map-icon-picker.dialog.html',
  styleUrl: './interactive-map-icon-picker.dialog.scss',
  // Default CD — dialog overlay + Transloco require it.
  changeDetection: ChangeDetectionStrategy.Default,
})
export class InteractiveMapIconPickerDialogComponent {
  readonly segment = signal<Segment>('library');
  readonly search = signal('');
  readonly uploadedUrl = signal<string | null>(null);

  readonly filtered = computed(() => {
    const q = this.search().trim().toLowerCase().replace(/\s+/g, '-');
    return q ? MAP_ICON_KEYS.filter((k) => k.includes(q)) : MAP_ICON_KEYS;
  });

  constructor(
    private readonly ref: MatDialogRef<InteractiveMapIconPickerDialogComponent, string | undefined>,
    @Inject(MAT_DIALOG_DATA) readonly data: IconPickerData,
  ) {
    // If the node already uses an uploaded icon, open on the Upload tab.
    if (isCustomIconUrl(data.current)) {
      this.segment.set('upload');
      this.uploadedUrl.set(data.current);
    }
  }

  setSegment(seg: Segment): void { this.segment.set(seg); }

  /** Library icon preview (cyan line icon on a dark tile). */
  preview(key: string): string { return iconDataUri(key); }

  /** Human label for a registry key: "arrow-down-right" → "Arrow down right". */
  prettify(key: string): string {
    const s = key.replace(/-/g, ' ');
    return s.charAt(0).toUpperCase() + s.slice(1);
  }

  isCurrent(key: string): boolean { return this.data.current === key; }

  select(key: string): void { this.ref.close(key); }

  onUploaded(asset: AssetFile): void { this.uploadedUrl.set(asset.url); }

  useUploaded(): void {
    const url = this.uploadedUrl();
    if (url) this.ref.close(url);
  }

  cancel(): void { this.ref.close(undefined); }
}
