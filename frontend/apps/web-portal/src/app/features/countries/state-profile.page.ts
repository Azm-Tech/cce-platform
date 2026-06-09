import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { CountriesApiService } from './countries-api.service';
import { StateProfileEditDialogComponent } from './state-profile-edit.dialog';
import type { StateProfile } from './country.types';

@Component({
  selector: 'cce-state-profile',
  standalone: true,
  imports: [DatePipe, DecimalPipe, MatButtonModule, MatIconModule, TranslocoModule],
  templateUrl: './state-profile.page.html',
  styleUrl: './state-profile.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StateProfilePage implements OnInit {
  private readonly api = inject(CountriesApiService);
  private readonly dialog = inject(MatDialog);
  readonly locale = inject(LocaleService).locale;

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly profile = signal<StateProfile | null>(null);

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  async retry(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    await this.load();
  }

  openEdit(): void {
    const current = this.profile();
    if (!current) return;
    const ref = this.dialog.open<StateProfileEditDialogComponent, StateProfile, StateProfile | null>(
      StateProfileEditDialogComponent,
      { data: current, width: '680px', maxWidth: '95vw', disableClose: true, panelClass: 'cce-dialog-no-padding' },
    );
    ref.afterClosed().subscribe((updated) => {
      if (updated) this.profile.set(updated);
    });
  }

  private async load(): Promise<void> {
    const res = await this.api.getStateProfile();
    this.loading.set(false);
    if (res.ok) this.profile.set(res.value);
    else this.error.set(res.error.kind);
  }
}
