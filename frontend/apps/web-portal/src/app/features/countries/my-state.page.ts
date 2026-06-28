import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslocoModule } from '@jsverse/transloco';
import { CountriesApiService } from './countries-api.service';

@Component({
  selector: 'cce-my-state',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule, TranslocoModule],
  template: `
    <div class="cce-my-state">
      @if (loading()) {
        <mat-spinner diameter="48" />
        <p class="cce-my-state__msg">{{ 'common.loading' | transloco }}</p>
      } @else if (noCountry()) {
        <mat-icon class="cce-my-state__icon">info_outline</mat-icon>
        <p class="cce-my-state__msg">{{ 'countries.myState.noCountry' | transloco }}</p>
        <a routerLink="/countries" mat-stroked-button color="primary">
          {{ 'countries.back' | transloco }}
        </a>
      } @else if (errorKind()) {
        <mat-icon class="cce-my-state__icon cce-my-state__icon--error">error_outline</mat-icon>
        <p class="cce-my-state__msg">{{ 'errors.ERR001' | transloco }}</p>
        <button type="button" mat-flat-button color="primary" (click)="retry()">
          {{ 'errors.retry' | transloco }}
        </button>
      }
    </div>
  `,
  styles: [`
    .cce-my-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 1rem;
      min-height: 60vh;
      padding: 2rem;
      text-align: center;
    }
    .cce-my-state__icon {
      font-size: 3rem;
      width: 3rem;
      height: 3rem;
      color: var(--mat-sys-primary);
    }
    .cce-my-state__icon--error {
      color: var(--mat-sys-error);
    }
    .cce-my-state__msg {
      color: var(--mat-sys-on-surface-variant);
      margin: 0;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyStatePage implements OnInit {
  private readonly countriesApi = inject(CountriesApiService);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly noCountry = signal(false);
  readonly errorKind = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  async retry(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    this.noCountry.set(false);
    await this.load();
  }

  private async load(): Promise<void> {
    const res = await this.countriesApi.getStateProfile();
    this.loading.set(false);
    if (res.ok) {
      await this.router.navigate(['/countries', res.value.countryId], { replaceUrl: true });
    } else if (res.error.kind === 'not-found') {
      this.noCountry.set(true);
    } else {
      this.errorKind.set(res.error.kind);
    }
  }
}
