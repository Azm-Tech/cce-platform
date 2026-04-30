import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslateModule } from '@ngx-translate/core';
import { PermissionDirective } from '../../core/auth/permission.directive';
import { ToastService } from '@frontend/ui-kit';
import { REPORTS, type ReportConfig } from './reports-config';
import { ReportsApiService } from './reports-api.service';

/**
 * Admin → Reports landing page. Renders a card per ReportConfig; each card
 * is permission-gated and exposes from/to date inputs + a Download button.
 */
@Component({
  selector: 'cce-reports',
  standalone: true,
  imports: [
    CommonModule, FormsModule,
    MatButtonModule, MatCardModule, MatFormFieldModule, MatIconModule,
    MatInputModule, MatProgressSpinnerModule, TranslateModule, PermissionDirective,
  ],
  templateUrl: './reports.page.html',
  styleUrl: './reports.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportsPage {
  private readonly api = inject(ReportsApiService);
  private readonly toast = inject(ToastService);

  readonly reports = REPORTS;
  readonly busy = signal<string | null>(null);
  readonly fromInputs = signal<Record<string, string>>({});
  readonly toInputs = signal<Record<string, string>>({});

  setFrom(slug: string, value: string): void {
    this.fromInputs.update((s) => ({ ...s, [slug]: value }));
  }
  setTo(slug: string, value: string): void {
    this.toInputs.update((s) => ({ ...s, [slug]: value }));
  }
  fromOf(slug: string): string {
    return this.fromInputs()[slug] ?? '';
  }
  toOf(slug: string): string {
    return this.toInputs()[slug] ?? '';
  }

  async download(report: ReportConfig): Promise<void> {
    this.busy.set(report.slug);
    const res = await this.api.download(report.slug, {
      from: this.fromOf(report.slug) || undefined,
      to: this.toOf(report.slug) || undefined,
    });
    this.busy.set(null);
    if (res.ok) {
      this.saveBlob(res.value, this.filenameFor(report.slug));
      this.toast.success('reports.download.toast');
    } else {
      this.toast.error(`errors.${res.error.kind}`);
    }
  }

  private filenameFor(slug: string): string {
    const today = new Date().toISOString().slice(0, 10);
    return `${slug}-${today}.csv`;
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
