import { inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import type { AnyCity, City, CityMin, FeaturedCity } from './world-map.types';

@Injectable({ providedIn: 'root' })
export class CitiesService {
  private readonly http = inject(HttpClient);

  private readonly _allCities = signal<readonly AnyCity[]>([]);
  readonly allCities = this._allCities.asReadonly();

  private loadPromise: Promise<void> | null = null;

  /** Idempotent: fetches city JSON once per session then caches in a signal. */
  ensureLoaded(): Promise<void> {
    if (this._allCities().length > 0) return Promise.resolve();
    if (this.loadPromise) return this.loadPromise;
    this.loadPromise = this.fetchCities();
    return this.loadPromise;
  }

  private async fetchCities(): Promise<void> {
    const [featured, extra] = await Promise.all([
      firstValueFrom(this.http.get<City[]>('/assets/data/cities.json')),
      firstValueFrom(this.http.get<CityMin[]>('/assets/data/cities-extra.json')),
    ]);
    const all: AnyCity[] = [
      ...featured.map((c): FeaturedCity => ({ ...c, kind: 'featured' })),
      ...extra,
    ];
    this._allCities.set(all);
  }
}
