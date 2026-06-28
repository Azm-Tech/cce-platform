import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ToastService } from '@frontend/ui-kit';
import { MatDialog } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { LocaleService } from '@frontend/i18n';
import { TranslocoModule } from '@jsverse/transloco';
import { CountriesApiService } from './countries-api.service';
import { MediaApiService } from '../../core/media/media-api.service';
import { SharePostDialogComponent, type SharePostDialogData } from '../community/share-post-dialog.component';
import { getMockAchievements, getMockCardStats, getMockCountryMeta, getMockKapsarc } from './testing/countries-mock';
import { flagEmojiFor, flagUrlFor } from './flag-helpers';
import type { Country, CountryAchievement, CountryMeta, CountryProfile, KapsarcSnapshot } from './country.types';

type Tab = 'analytics' | 'experts';

interface RadarAxis {
  labelKey: string;
  score: number;
  x: number;
  y: number;
  labelX: number;
  labelY: number;
}

interface TrendPoint { year: number; score: number; x: number; y: number; }

const RADAR_CX = 140;
const RADAR_CY = 140;
const RADAR_R  = 100;
const CHART_W  = 280;
const CHART_H  = 280;

const TREND_W = 320;
const TREND_H = 180;
const TREND_PAD_L = 32;
const TREND_PAD_B = 24;
const TREND_PAD_T = 16;

@Component({
  selector: 'cce-country-detail',
  standalone: true,
  imports: [
    DecimalPipe, RouterLink,
    MatButtonModule, MatProgressBarModule, MatIconModule, MatTabsModule, MatTooltipModule,
    TranslocoModule,
  ],
  templateUrl: './country-detail.page.html',
  styleUrl: './country-detail.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CountryDetailPage implements OnInit {
  private readonly countriesApi = inject(CountriesApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly localeService = inject(LocaleService);
  private readonly toast = inject(ToastService);
  private readonly media = inject(MediaApiService);
  private readonly dialog = inject(MatDialog);

  /** Open the country's NDC document (by asset id) in a new tab. */
  async viewNdc(assetId: string): Promise<void> {
    const res = await this.media.getAsset(assetId);
    if (res.ok) window.open(res.value.url, '_blank', 'noopener');
    else this.toast.error(`errors.${res.error.kind}`);
  }

  readonly country = signal<Country | null>(null);
  readonly profile = signal<CountryProfile | null>(null);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly flagFailed = signal(false);
  readonly activeTab = signal<Tab>('analytics');
  readonly downloadOpen = signal(false);
  readonly tabList: Tab[] = ['analytics', 'experts'];
  readonly detailSkeletons = Array.from({ length: 6 });

  readonly mockHint = computed(() =>
    this.locale() === 'ar'
      ? 'بيانات تجريبية — ستُستبدل ببيانات حقيقية من الـ API لاحقاً'
      : 'Static mock data — will be replaced with real API data',
  );

  readonly locale = this.localeService.locale;

  readonly flagSrc = computed(() => {
    const c = this.country();
    return c ? flagUrlFor(c) : '';
  });

  readonly flagEmoji = computed(() => {
    const c = this.country();
    return c ? flagEmojiFor(c.isoAlpha2) : '🏳️';
  });

  readonly headerName = computed(() => {
    const c = this.country();
    if (!c) return '';
    return this.locale() === 'ar' ? c.nameAr : c.nameEn;
  });

  readonly headerRegion = computed(() => {
    const c = this.country();
    if (!c) return '';
    return this.locale() === 'ar' ? c.regionAr : c.regionEn;
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

  /** KapsarcSnapshot derived from real profile data + mock sub-scores (sub-scores not yet in API). */
  readonly snapshot = computed<KapsarcSnapshot | null>(() => {
    const p = this.profile();
    const c = this.country();
    const mockSnap = c ? getMockKapsarc(c) : null;
    if (p?.cceTotalIndex != null) {
      return {
        id: '',
        countryId: p.countryId,
        classification: p.cceClassification ?? mockSnap?.classification ?? '',
        performanceScore: p.ccePerformanceScore ?? mockSnap?.performanceScore ?? 0,
        totalIndex: p.cceTotalIndex,
        snapshotTakenOn: p.cceSnapshotTakenOn ?? '',
        sourceVersion: null,
        subScores: mockSnap?.subScores,
        trendYoY: mockSnap?.trendYoY,
      };
    }
    return mockSnap;
  });

  readonly countryMeta = computed<CountryMeta | null>(() => {
    const p = this.profile();
    const c = this.country();
    if (!p && !c) return null;
    const mock = c ? getMockCountryMeta(c) : null;
    return {
      populationMillions: p?.population != null ? p.population / 1_000_000 : (mock?.populationMillions ?? 0),
      populationTrend:      mock?.populationTrend ?? 0,
      areaMillionKm2:       p?.areaSqKm != null ? p.areaSqKm / 1_000_000 : (mock?.areaMillionKm2 ?? 0),
      administrativeDivisions: mock?.administrativeDivisions ?? 0,
      gdpPerCapita:         p?.gdpPerCapita ?? (mock?.gdpPerCapita ?? 0),
      gdpTrend:             mock?.gdpTrend ?? 0,
      energyDensityPerMJ:   mock?.energyDensityPerMJ ?? 0,
      isFoundingPartner:    mock?.isFoundingPartner ?? false,
    };
  });

  readonly cceRankDisplay = computed(() => {
    const index = this.profile()?.cceTotalIndex ?? this.snapshot()?.totalIndex;
    if (index == null) return null;
    return Math.max(1, 126 - Math.round(index));
  });

  readonly cardStats = computed(() => {
    const p = this.profile();
    const c = this.country();
    if (!p && !c) return null;
    const mock = c ? getMockCardStats(c) : null;
    return {
      emissionReductionPct: mock?.emissionReductionPct ?? 0,
      emissionTrend:        mock?.emissionTrend ?? ('flat' as const),
      globalRank:           mock?.globalRank ?? 0,
      totalCountries:       mock?.totalCountries ?? 125,
      cceClassification:    p?.cceClassification ?? mock?.cceClassification ?? '',
    };
  });

  readonly achievements = computed<CountryAchievement[]>(() => {
    const c = this.country();
    return c ? getMockAchievements(c) : [];
  });

  // ─── Radar chart (4Rs) ────────────────────────────────
  readonly radarAxes = computed<RadarAxis[]>(() => {
    const sub = this.snapshot()?.subScores;
    if (!sub) return [];
    const axes = [
      { labelKey: 'countries.detail.reduce',  score: Math.round((sub.power + sub.industry) / 2) },
      { labelKey: 'countries.detail.reuse',   score: Math.round(sub.buildings) },
      { labelKey: 'countries.detail.recycle', score: Math.round(sub.transport) },
      { labelKey: 'countries.detail.remove',  score: Math.round(sub.landUse) },
    ];
    return axes.map((a, i) => {
      const angleDeg = i * 90 - 90;
      const rad = angleDeg * (Math.PI / 180);
      const cos = Math.cos(rad);
      const sin = Math.sin(rad);
      return {
        ...a,
        x: RADAR_CX + RADAR_R * (a.score / 100) * cos,
        y: RADAR_CY + RADAR_R * (a.score / 100) * sin,
        labelX: RADAR_CX + (RADAR_R + 22) * cos,
        labelY: RADAR_CY + (RADAR_R + 22) * sin,
      };
    });
  });

  readonly radarPolygon = computed<string>(() =>
    this.radarAxes().map(a => `${a.x},${a.y}`).join(' '),
  );

  readonly radarGridLines = computed<string[]>(() => {
    const n = 4;
    return Array.from({ length: n }, (_, i) => {
      const r = RADAR_R * ((i + 1) / n);
      const angles = [0, 90, 180, 270].map(d => d * (Math.PI / 180) - Math.PI / 2);
      return angles.map((rad, j) => {
        const x = RADAR_CX + r * Math.cos(rad);
        const y = RADAR_CY + r * Math.sin(rad);
        return `${j === 0 ? 'M' : 'L'}${x},${y}`;
      }).join(' ') + 'Z';
    });
  });

  readonly radarAxisLines = computed<Array<{x2: number; y2: number}>>(() =>
    [0, 90, 180, 270].map(deg => {
      const rad = (deg - 90) * (Math.PI / 180);
      return { x2: RADAR_CX + RADAR_R * Math.cos(rad), y2: RADAR_CY + RADAR_R * Math.sin(rad) };
    }),
  );

  readonly radarScoreLabels = computed<Array<{labelKey: string; score: number; x: number; y: number}>>(() =>
    this.radarAxes().map(a => ({ labelKey: a.labelKey, score: a.score, x: a.x, y: a.y })),
  );

  // ─── Trend line chart ─────────────────────────────────
  readonly trendPoints = computed<TrendPoint[]>(() => {
    const snap = this.snapshot();
    if (!snap) return [];
    const endScore = snap.totalIndex;
    const yoy = snap.trendYoY ?? 1;
    const jitter = [0, -1.2, 0.8, -0.5, 1.5, -0.3, 0];
    const raw = Array.from({ length: 7 }, (_, i) => ({
      year: 2020 + i,
      score: Math.max(10, Math.min(100, endScore - yoy * (6 - i) + jitter[i])),
    }));
    const minS = Math.min(...raw.map(p => p.score));
    const maxS = Math.max(...raw.map(p => p.score));
    const range = Math.max(maxS - minS, 5);
    const innerW = TREND_W - TREND_PAD_L;
    const innerH = TREND_H - TREND_PAD_B - TREND_PAD_T;
    return raw.map((p, i) => ({
      ...p,
      x: TREND_PAD_L + (i / 6) * innerW,
      y: TREND_PAD_T + innerH - ((p.score - minS) / range) * innerH,
    }));
  });

  readonly trendPolyline = computed<string>(() =>
    this.trendPoints().map(p => `${p.x},${p.y}`).join(' '),
  );

  readonly trendFill = computed<string>(() => {
    const pts = this.trendPoints();
    if (!pts.length) return '';
    const last = pts[pts.length - 1];
    const bottomY = TREND_H - TREND_PAD_B;
    return `M${pts[0].x},${bottomY} ` +
      pts.map(p => `L${p.x},${p.y}`).join(' ') +
      ` L${last.x},${bottomY}Z`;
  });

  readonly chartW = CHART_W;
  readonly chartH = CHART_H;
  readonly radarCx = RADAR_CX;
  readonly radarCy = RADAR_CY;
  readonly radarR = RADAR_R;
  readonly trendW = TREND_W;
  readonly trendH = TREND_H;
  readonly trendPadL = TREND_PAD_L;
  readonly trendPadB = TREND_PAD_B;

  onFlagError(): void {
    this.flagFailed.set(true);
  }

  share(): void {
    this.dialog.open<SharePostDialogComponent, SharePostDialogData>(
      SharePostDialogComponent,
      {
        data: { url: window.location.href, title: this.headerName() },
        width: '480px',
        maxWidth: '95vw',
        autoFocus: false,
        panelClass: 'cce-share-dialog',
      },
    );
  }

  setTab(tab: Tab): void {
    this.activeTab.set(tab);
  }

  toggleDownload(): void {
    this.downloadOpen.update(v => !v);
  }

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.errorKind.set('not-found'); return; }
    this.loading.set(true);

    // The profile endpoint already returns the header data (name / flag /
    // iso), so we only fetch it — no separate /api/countries/{id} call.
    const profileRes = await this.countriesApi.getProfile(id);
    this.loading.set(false);

    if (profileRes.ok) {
      const p = profileRes.value;
      this.profile.set(p);
      const iso2 = p.flagUrl?.match(/\/([a-z]{2})\.png/i)?.[1]?.toUpperCase() ?? '';
      this.country.set({
        id, isoAlpha3: p.isoAlpha3, isoAlpha2: iso2,
        nameAr: p.nameAr, nameEn: p.nameEn,
        regionAr: '', regionEn: '',
        flagUrl: p.flagUrl ?? '',
        dialCode: '', isCceCountry: false,
        cceClassification: null, ccePerformanceScore: null, cceTotalIndex: null,
      });
    } else {
      this.errorKind.set(profileRes.error.kind);
    }
  }
}
