import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslateModule } from '@ngx-translate/core';
import { ToastService } from '@frontend/ui-kit';
import { LocaleService } from '@frontend/i18n';
import { AuthService } from '../../../core/auth/auth.service';
import { ScenarioBuilderStore } from './scenario-builder-store.service';
import {
  SaveScenarioDialogComponent,
  type SaveScenarioDialogData,
  type SaveScenarioDialogResult,
} from './save-scenario-dialog.component';

/**
 * Sticky bottom bar with live totals + Run + Save buttons + server
 * summary. Run posts the current configuration to the server and swaps
 * the live numbers for the authoritative result + a localized summary.
 * Save is auth-gated: anonymous users get bumped to sign-in; signed-in
 * users see a name-confirmation dialog before the POST.
 */
@Component({
  selector: 'cce-totals-bar',
  standalone: true,
  imports: [
    DecimalPipe,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    TranslateModule,
  ],
  templateUrl: './totals-bar.component.html',
  styleUrl: './totals-bar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TotalsBarComponent {
  private readonly store = inject(ScenarioBuilderStore);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly toast = inject(ToastService);
  private readonly localeService = inject(LocaleService);

  readonly liveTotals = this.store.liveTotals;
  readonly serverResult = this.store.serverResult;
  readonly canRun = this.store.canRun;
  readonly canSave = this.store.canSave;
  readonly running = this.store.running;
  readonly saving = this.store.saving;
  readonly locale = this.localeService.locale;

  serverSummary(): string | null {
    const r = this.serverResult();
    if (!r) return null;
    return this.locale() === 'ar' ? r.summaryAr : r.summaryEn;
  }

  async runScenario(): Promise<void> {
    if (!this.canRun()) return;
    const res = await this.store.run();
    if (res.ok) {
      this.toast.success('interactiveCity.toasts.runOk');
    } else {
      this.toast.error('interactiveCity.errors.runFailed');
    }
  }

  async saveScenario(): Promise<void> {
    if (!this.canSave()) return;

    if (!this.auth.isAuthenticated()) {
      this.auth.signIn();
      return;
    }

    const data: SaveScenarioDialogData = { initialName: this.store.name() };
    const dialogRef = this.dialog.open<
      SaveScenarioDialogComponent,
      SaveScenarioDialogData,
      SaveScenarioDialogResult
    >(SaveScenarioDialogComponent, { data, width: '420px' });
    const result = await firstValueFrom(dialogRef.afterClosed());
    if (!result || !result.name) return;

    this.store.setName(result.name);
    const res = await this.store.save();
    if (res.ok) {
      this.toast.success('interactiveCity.toasts.saveOk');
    } else {
      this.toast.error('interactiveCity.errors.saveFailed');
    }
  }
}
