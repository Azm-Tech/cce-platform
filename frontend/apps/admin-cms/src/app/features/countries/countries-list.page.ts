import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { TranslateModule } from '@ngx-translate/core';
import { CountryApiService } from './country-api.service';
import type { Country } from './country.types';

@Component({
  selector: 'cce-countries-list',
  standalone: true,
  imports: [
    CommonModule, FormsModule, RouterLink,
    MatButtonModule, MatFormFieldModule, MatInputModule,
    MatPaginatorModule, MatProgressBarModule, MatTableModule, TranslateModule,
  ],
  templateUrl: './countries-list.page.html',
  styleUrl: './countries.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CountriesListPage implements OnInit {
  private readonly api = inject(CountryApiService);

  readonly displayedColumns = ['flag', 'iso', 'nameEn', 'region', 'isActive', 'actions'];
  readonly searchInput = signal('');
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly rows = signal<Country[]>([]);
  readonly total = signal(0);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  ngOnInit(): void { void this.load(); }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.listCountries({
      page: this.page(),
      pageSize: this.pageSize(),
      search: this.searchInput() || undefined,
    });
    this.loading.set(false);
    if (res.ok) {
      this.rows.set(res.value.items);
      this.total.set(Number(res.value.total));
    } else this.errorKind.set(res.error.kind);
  }

  onPage(e: PageEvent): void {
    this.page.set(e.pageIndex + 1);
    this.pageSize.set(e.pageSize);
    void this.load();
  }
  onSearch(): void { this.page.set(1); void this.load(); }
}
