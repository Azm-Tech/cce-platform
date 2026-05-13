import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSliderModule } from '@angular/material/slider';
import { TranslateModule } from '@ngx-translate/core';
import { ENV_FACTOR_BOUNDS, type EnvironmentalFactors } from '../interactive-city.types';
import { ScenarioBuilderStore } from './scenario-builder-store.service';

interface FactorRow {
  /** Field name on the EnvironmentalFactors object. */
  key: keyof EnvironmentalFactors;
  /** Material icon shown on the row. */
  icon: string;
  /** i18n key for the row's title. */
  labelKey: string;
  /** i18n key for the help line under the slider. */
  helpKey: string;
  /** Min/max/step + unit (used for ARIA + the value badge). */
  bounds: typeof ENV_FACTOR_BOUNDS[keyof EnvironmentalFactors];
}

/**
 * F009: Environmental Factors panel.
 *
 * Six sliders the user adjusts to describe the governorate's current
 * environmental profile. The store recomputes the baseline carbon
 * footprint synchronously on every change, which feeds the totals bar
 * and the carbon-neutrality progress display.
 *
 * Modern + simple: two-column grid on desktop, single column on mobile.
 * Each row is a card with icon + label + value badge + brand-tinted
 * Material slider + a one-line help description.
 */
@Component({
  selector: 'cce-environmental-factors',
  standalone: true,
  imports: [
    DecimalPipe, FormsModule,
    MatButtonModule, MatIconModule, MatSliderModule,
    TranslateModule,
  ],
  templateUrl: './environmental-factors.component.html',
  styleUrl: './environmental-factors.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EnvironmentalFactorsComponent {
  private readonly store = inject(ScenarioBuilderStore);

  readonly factors = this.store.envFactors;
  readonly baseline = this.store.baselineKgPerYear;

  readonly rows: readonly FactorRow[] = [
    {
      key: 'publicTransportPct',
      icon: 'directions_bus',
      labelKey: 'interactiveCity.factors.publicTransportPct',
      helpKey: 'interactiveCity.factors.publicTransportPctHelp',
      bounds: ENV_FACTOR_BOUNDS.publicTransportPct,
    },
    {
      key: 'avgTransportKmPerDay',
      icon: 'route',
      labelKey: 'interactiveCity.factors.avgTransportKmPerDay',
      helpKey: 'interactiveCity.factors.avgTransportKmPerDayHelp',
      bounds: ENV_FACTOR_BOUNDS.avgTransportKmPerDay,
    },
    {
      key: 'renewableEnergyPct',
      icon: 'solar_power',
      labelKey: 'interactiveCity.factors.renewableEnergyPct',
      helpKey: 'interactiveCity.factors.renewableEnergyPctHelp',
      bounds: ENV_FACTOR_BOUNDS.renewableEnergyPct,
    },
    {
      key: 'wasteRecyclingPct',
      icon: 'recycling',
      labelKey: 'interactiveCity.factors.wasteRecyclingPct',
      helpKey: 'interactiveCity.factors.wasteRecyclingPctHelp',
      bounds: ENV_FACTOR_BOUNDS.wasteRecyclingPct,
    },
    {
      key: 'greenSpacePct',
      icon: 'park',
      labelKey: 'interactiveCity.factors.greenSpacePct',
      helpKey: 'interactiveCity.factors.greenSpacePctHelp',
      bounds: ENV_FACTOR_BOUNDS.greenSpacePct,
    },
    {
      key: 'industrialIntensity',
      icon: 'factory',
      labelKey: 'interactiveCity.factors.industrialIntensity',
      helpKey: 'interactiveCity.factors.industrialIntensityHelp',
      bounds: ENV_FACTOR_BOUNDS.industrialIntensity,
    },
  ];

  /** True when any factor differs from the default — drives the
   *  "Reset" button's disabled state. */
  readonly hasChanges = computed(() => {
    const f = this.factors();
    return (
      f.publicTransportPct !== 30 ||
      f.avgTransportKmPerDay !== 35 ||
      f.renewableEnergyPct !== 15 ||
      f.wasteRecyclingPct !== 20 ||
      f.greenSpacePct !== 25 ||
      f.industrialIntensity !== 60
    );
  });

  setValue(key: keyof EnvironmentalFactors, raw: number | null | undefined): void {
    if (raw === null || raw === undefined || !Number.isFinite(raw)) return;
    this.store.setEnvFactor(key, Math.round(raw));
  }

  reset(): void {
    this.store.resetEnvFactors();
  }
}
