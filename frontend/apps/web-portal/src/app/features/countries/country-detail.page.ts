import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatIconModule } from '@angular/material/icon';
import { LocaleService } from '@frontend/i18n';
import { TranslateModule } from '@ngx-translate/core';
import { CountriesApiService } from './countries-api.service';
import { KapsarcApiService } from './kapsarc-api.service';
import { KapsarcSnapshotComponent } from './kapsarc-snapshot.component';
import type { Country, CountryProfile, KapsarcSnapshot } from './country.types';

@Component({
  selector: 'cce-country-detail',
  standalone: true,
  imports: [
    CommonModule, DatePipe, RouterLink,
    MatButtonModule, MatProgressBarModule, MatIconModule,
    TranslateModule, KapsarcSnapshotComponent,
  ],
  templateUrl: './country-detail.page.html',
  styleUrl: './country-detail.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CountryDetailPage implements OnInit {
  private readonly countriesApi = inject(CountriesApiService);
  private readonly kapsarcApi = inject(KapsarcApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly localeService = inject(LocaleService);

  readonly country = signal<Country | null>(null);
  readonly profile = signal<CountryProfile | null>(null);
  readonly snapshot = signal<KapsarcSnapshot | null>(null);
  readonly snapshotErrorKind = signal<string | null>(null);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  readonly locale = this.localeService.locale;

  readonly headerName = computed(() => {
    const c = this.country();
    if (!c) return '';
    return this.locale() === 'ar' ? c.nameAr : c.nameEn;
  });

  readonly description = computed(() => {
    const p = this.profile();
    if (!p) return '';
    return this.locale() === 'ar' ? p.descriptionAr : p.descriptionEn;
  });

  readonly keyInitiatives = computed(() => {
    const p = this.profile();
    if (!p) return '';
    return this.locale() === 'ar' ? p.keyInitiativesAr : p.keyInitiativesEn;
  });

  readonly contactInfo = computed(() => {
    const p = this.profile();
    if (!p) return null;
    return this.locale() === 'ar' ? p.contactInfoAr : p.contactInfoEn;
  });

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.errorKind.set('not-found');
      return;
    }
    this.loading.set(true);
    this.errorKind.set(null);
    this.snapshotErrorKind.set(null);

    const [profileRes, snapshotRes, listRes] = await Promise.all([
      this.countriesApi.getProfile(id),
      this.kapsarcApi.getLatestSnapshot(id),
      this.countriesApi.listCountries({}),
    ]);
    this.loading.set(false);

    if (profileRes.ok) this.profile.set(profileRes.value);
    else this.errorKind.set(profileRes.error.kind);

    if (snapshotRes.ok) this.snapshot.set(snapshotRes.value);
    else this.snapshotErrorKind.set(snapshotRes.error.kind);

    // Country header info comes from listCountries; tolerate failure (no header).
    if (listRes.ok) {
      const match = listRes.value.find((c) => c.id === id);
      if (match) this.country.set(match);
    }
  }
}
