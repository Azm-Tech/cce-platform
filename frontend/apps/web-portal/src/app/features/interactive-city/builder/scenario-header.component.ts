import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, effect, inject } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormBuilder, FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TranslocoModule } from '@jsverse/transloco';
import { CITY_TYPES, targetYearBounds, type CityType } from '../interactive-city.types';
import { ScenarioBuilderStore } from './scenario-builder-store.service';

/**
 * Header strip — name + city-type + target-year inputs. Two-way binds
 * a Reactive `FormGroup` to the store: store signals → form patch (via
 * an effect on hydrate / load), form valueChanges → store actions.
 */
@Component({
  selector: 'cce-scenario-header',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatAutocompleteModule,
    MatFormFieldModule,
    MatInputModule,
    TranslocoModule,
  ],
  templateUrl: './scenario-header.component.html',
  styleUrl: './scenario-header.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScenarioHeaderComponent implements OnInit {
  private readonly store = inject(ScenarioBuilderStore);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly cityTypes = CITY_TYPES;
  readonly yearBounds = targetYearBounds();

  readonly cityTypeSearch = new FormControl('');
  private readonly cityTypeSearchValue = toSignal(this.cityTypeSearch.valueChanges, { initialValue: '' });
  readonly filteredCityTypes = computed(() => {
    const q = (this.cityTypeSearchValue() ?? '').trim().toLowerCase();
    if (!q) return this.cityTypes;
    return this.cityTypes.filter(c => c.toLowerCase().includes(q));
  });

  readonly form = this.fb.nonNullable.group({
    name: this.store.name(),
    cityType: this.store.cityType(),
    targetYear: this.store.targetYear(),
  });

  constructor() {
    // Effect 1: store → form (so URL hydrate / loadFromSaved updates inputs).
    effect(() => {
      const next = {
        name: this.store.name(),
        cityType: this.store.cityType(),
        targetYear: this.store.targetYear(),
      };
      // emitEvent:false avoids re-triggering the form → store path.
      this.form.patchValue(next, { emitEvent: false });
      this.cityTypeSearch.setValue(this.store.cityType(), { emitEvent: false });
    });
  }

  onCityTypeSelected(value: CityType): void {
    this.form.controls.cityType.setValue(value, { emitEvent: false });
    this.cityTypeSearch.setValue(value, { emitEvent: false });
    this.store.setCityType(value);
  }

  ngOnInit(): void {
    // Form → store (one valueChanges → matching action per field).
    this.form.controls.name.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((v) => this.store.setName(v ?? ''));
    this.form.controls.cityType.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((v) => this.store.setCityType((v ?? 'Mixed') as CityType));
    this.form.controls.targetYear.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((v) => {
        if (typeof v !== 'number' || Number.isNaN(v)) return;
        const clamped = Math.min(Math.max(v, this.yearBounds.min), this.yearBounds.max);
        this.store.setTargetYear(clamped);
      });
  }
}
