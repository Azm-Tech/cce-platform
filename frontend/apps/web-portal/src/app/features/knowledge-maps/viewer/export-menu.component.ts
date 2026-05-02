import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { TranslateModule } from '@ngx-translate/core';

export type ExportFormat = 'png' | 'svg' | 'json' | 'pdf';

const EXPORT_FORMATS: readonly ExportFormat[] = ['png', 'svg', 'json', 'pdf'];

/**
 * Mat-menu trigger labelled "Export" with one item per supported
 * format. Click an item -> emits (formatChosen) with the format
 * string. Parent dispatches to the right serializer + downloadBlob
 * (Phase 6.5 wires this up).
 *
 * Disabled when the parent flags it (e.g., active tab not loaded yet).
 */
@Component({
  selector: 'cce-export-menu',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatMenuModule, TranslateModule],
  templateUrl: './export-menu.component.html',
  styleUrl: './export-menu.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExportMenuComponent {
  readonly disabled = input<boolean>(false);
  readonly formatChosen = output<ExportFormat>();

  readonly formats = EXPORT_FORMATS;

  onChoose(format: ExportFormat): void {
    if (this.disabled()) return;
    this.formatChosen.emit(format);
  }
}
