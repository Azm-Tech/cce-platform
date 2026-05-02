import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import type { CityTechnology } from '../interactive-city.types';
import { ScenarioBuilderStore } from './scenario-builder-store.service';

/**
 * Right-rail "cart" of selected technologies. Click × to remove, click
 * "Clear all" to wipe. Empty state pushes the user back to the catalog.
 */
@Component({
  selector: 'cce-selected-list',
  standalone: true,
  imports: [DecimalPipe, MatButtonModule, MatIconModule, TranslateModule],
  templateUrl: './selected-list.component.html',
  styleUrl: './selected-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SelectedListComponent {
  private readonly store = inject(ScenarioBuilderStore);
  private readonly localeService = inject(LocaleService);

  readonly locale = this.localeService.locale;
  readonly selectedTechnologies = this.store.selectedTechnologies;
  readonly count = this.store.selectedIds;

  nameOf(t: CityTechnology): string {
    return this.locale() === 'ar' ? t.nameAr : t.nameEn;
  }

  remove(id: string): void {
    this.store.toggle(id);
  }

  clearAll(): void {
    this.store.clear();
  }
}
