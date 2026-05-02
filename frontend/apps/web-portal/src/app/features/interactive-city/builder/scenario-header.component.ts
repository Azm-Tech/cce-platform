import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, effect, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { TranslateModule } from '@ngx-translate/core';
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
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    TranslateModule,
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
    });
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
