import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslateModule } from '@ngx-translate/core';
import { ToastService } from '@frontend/ui-kit';
import { LocaleService } from '@frontend/i18n';
import { AuthService } from '../../../core/auth/auth.service';
import type { SavedScenario } from '../interactive-city.types';
import { ScenarioBuilderStore } from './scenario-builder-store.service';
import {
  ConfirmDialogComponent,
  type ConfirmDialogData,
} from './confirm-dialog.component';

/**
 * Auth-only saved-scenarios drawer. Lists previously saved scenarios
 * with Load + Delete actions. Anonymous users see a sign-in CTA card.
 * Loading while the current state is dirty triggers a confirmation
 * dialog ("Discard current changes?").
 */
@Component({
  selector: 'cce-saved-scenarios-drawer',
  standalone: true,
  imports: [
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    TranslateModule,
  ],
  templateUrl: './saved-scenarios-drawer.component.html',
  styleUrl: './saved-scenarios-drawer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SavedScenariosDrawerComponent {
  private readonly store = inject(ScenarioBuilderStore);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly toast = inject(ToastService);
  private readonly localeService = inject(LocaleService);

  readonly isAuthenticated = this.auth.isAuthenticated;
  readonly savedScenarios = this.store.savedScenarios;
  readonly savedLoading = this.store.savedLoading;
  readonly savedError = this.store.savedError;
  readonly locale = this.localeService.locale;

  signIn(): void {
    this.auth.signIn();
  }

  retry(): void {
    void this.store.init();
  }

  nameOf(s: SavedScenario): string {
    return this.locale() === 'ar' ? s.nameAr : s.nameEn;
  }

  async load(scenario: SavedScenario): Promise<void> {
    if (this.store.dirty()) {
      const ok = await this.openConfirm({
        titleKey: 'interactiveCity.builder.unsavedChangesTitle',
        bodyKey: 'interactiveCity.builder.unsavedChangesBody',
        confirmKey: 'interactiveCity.builder.unsavedChangesConfirm',
        cancelKey: 'interactiveCity.builder.unsavedChangesCancel',
        dangerous: false,
      });
      if (!ok) return;
    }
    this.store.loadFromSaved(scenario);
  }

  async remove(scenario: SavedScenario): Promise<void> {
    const ok = await this.openConfirm({
      titleKey: 'interactiveCity.saved.confirmDeleteTitle',
      bodyKey: 'interactiveCity.saved.confirmDeleteBody',
      bodyParams: { name: this.nameOf(scenario) },
      confirmKey: 'interactiveCity.saved.confirmDelete',
      cancelKey: 'interactiveCity.saved.confirmCancel',
      dangerous: true,
    });
    if (!ok) return;

    const res = await this.store.delete(scenario.id);
    if (res.ok) {
      this.toast.success('interactiveCity.toasts.deleteOk');
    } else {
      this.toast.error('interactiveCity.errors.deleteFailed');
    }
  }

  private async openConfirm(data: ConfirmDialogData): Promise<boolean> {
    const dialogRef = this.dialog.open<ConfirmDialogComponent, ConfirmDialogData, boolean>(
      ConfirmDialogComponent,
      { data, width: '420px' },
    );
    const result = await firstValueFrom(dialogRef.afterClosed());
    return result === true;
  }
}
