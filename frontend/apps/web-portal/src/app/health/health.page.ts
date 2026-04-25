import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslateModule } from '@ngx-translate/core';
import { HealthClient, type HealthResponse } from './health.client';

@Component({
  selector: 'cce-health-page',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatButtonModule, MatProgressSpinnerModule, TranslateModule],
  templateUrl: './health.page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HealthPage implements OnInit {
  private readonly client = inject(HealthClient);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly data = signal<HealthResponse | null>(null);

  refresh(): void {
    this.loading.set(true);
    this.error.set(null);
    this.client.fetch().subscribe({
      next: (resp) => {
        this.data.set(resp);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err?.message ?? 'unknown error');
        this.loading.set(false);
      },
    });
  }

  ngOnInit(): void {
    this.refresh();
  }
}
