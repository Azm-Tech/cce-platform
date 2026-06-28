
import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  Output,
  inject,
  signal,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslocoModule } from '@jsverse/transloco';
import { ContentApiService } from './content-api.service';
import type { AssetFile } from './content.types';

/**
 * Generic asset upload widget. Wraps a hidden file input + preview/replace UI.
 * Emits the uploaded {@link AssetFile} via `(uploaded)` so parent forms can pick
 * up the resulting `assetFileId` for resource creation.
 */
@Component({
  selector: 'cce-asset-upload',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, MatProgressBarModule, TranslocoModule],
  templateUrl: './asset-upload.component.html',
  styleUrl: './asset-upload.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AssetUploadComponent {
  private readonly api = inject(ContentApiService);

  readonly uploading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly asset = signal<AssetFile | null>(null);

  /** Comma-separated extension allow-list, e.g. ".pdf,.doc,.docx".
   *  Empty = accept anything (default, backwards compatible). */
  @Input() accept = '';

  @Output() readonly uploaded = new EventEmitter<AssetFile>();

  async onFile(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    // The accept attr is only a picker hint — drag-drop and "All files"
    // bypass it, so enforce the allow-list here too.
    if (this.accept && !this.isAllowed(file.name)) {
      input.value = '';
      this.errorKind.set('fileType');
      return;
    }
    this.uploading.set(true);
    this.errorKind.set(null);
    const res = await this.api.uploadAsset(file);
    this.uploading.set(false);
    // Reset the input so re-selecting the same file fires change again.
    input.value = '';
    if (res.ok) {
      this.asset.set(res.value);
      this.uploaded.emit(res.value);
    } else {
      this.errorKind.set(res.error.kind);
    }
  }

  clear(): void {
    this.asset.set(null);
    this.errorKind.set(null);
  }

  private isAllowed(filename: string): boolean {
    const ext = `.${filename.split('.').pop()?.toLowerCase() ?? ''}`;
    return this.accept
      .split(',')
      .map((e) => e.trim().toLowerCase())
      .includes(ext);
  }
}
