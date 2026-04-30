import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Output,
  inject,
  signal,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslateModule } from '@ngx-translate/core';
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
  imports: [CommonModule, MatButtonModule, MatIconModule, MatProgressBarModule, TranslateModule],
  templateUrl: './asset-upload.component.html',
  styleUrl: './asset-upload.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AssetUploadComponent {
  private readonly api = inject(ContentApiService);

  readonly uploading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly asset = signal<AssetFile | null>(null);

  @Output() readonly uploaded = new EventEmitter<AssetFile>();

  async onFile(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
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
}
