import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslateModule } from '@ngx-translate/core';
import { LocaleService } from '@frontend/i18n';
import { CountriesApiService } from '../countries/countries-api.service';
import type { Country } from '../countries/country.types';
import { AccountApiService } from './account-api.service';
import type { UserProfile } from './account.types';

@Component({
  selector: 'cce-profile-page',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule, MatChipsModule, MatIconModule, MatProgressBarModule,
    TranslateModule,
  ],
  templateUrl: './profile.page.html',
  styleUrl: './profile.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfilePage implements OnInit {
  private readonly api = inject(AccountApiService);
  private readonly countriesApi = inject(CountriesApiService);
  private readonly localeService = inject(LocaleService);

  readonly profile = signal<UserProfile | null>(null);
  readonly countries = signal<Country[]>([]);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  readonly locale = this.localeService.locale;

  readonly notProvisioned = computed(() => this.errorKind() === 'not-found');

  /** Resolves the user's countryId to a localized country name (or '—'). */
  readonly countryName = computed(() => {
    const p = this.profile();
    if (!p?.countryId) return '—';
    const match = this.countries().find((c) => c.id === p.countryId);
    if (!match) return '—';
    return this.locale() === 'ar' ? match.nameAr : match.nameEn;
  });

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const [profileRes, countriesRes] = await Promise.all([
      this.api.getProfile(),
      this.countriesApi.listCountries({}),
    ]);
    this.loading.set(false);
    if (profileRes.ok) this.profile.set(profileRes.value);
    else this.errorKind.set(profileRes.error.kind);
    if (countriesRes.ok) this.countries.set(countriesRes.value);
  }

  retry(): void {
    void this.load();
  }
}
