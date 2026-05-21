
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { ConfirmDialogService, ToastService } from '@frontend/ui-kit';
import type { FeatureError } from '@frontend/ui-kit';
import { CcePermission } from '@frontend/contracts';
import { CountryApiService } from '../countries/country-api.service';
import type { Country } from '../countries/country.types';
import { RoleLabelPipe } from './role-label.pipe';
import { IdentityApiService } from './identity-api.service';
import { PermissionDirective } from '../../core/auth/permission.directive';
import type { UserDetail } from './identity.types';

@Component({
  selector: 'cce-user-detail',
  standalone: true,
  imports: [
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatDividerModule,
    MatIconModule,
    MatProgressBarModule,
    TranslocoModule,
    PermissionDirective,
    RoleLabelPipe,
  ],
  templateUrl: './user-detail.page.html',
  styleUrl: './user-detail.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserDetailPage implements OnInit {
  private readonly api = inject(IdentityApiService);
  private readonly countryApi = inject(CountryApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmDialogService);
  readonly localeService = inject(LocaleService);

  readonly Permission = CcePermission;
  readonly user = signal<UserDetail | null>(null);
  readonly country = signal<Country | null>(null);
  readonly loading = signal(false);
  readonly error = signal<FeatureError | null>(null);

  readonly initials = computed(() => {
    const u = this.user();
    if (!u) return '?';
    const name = u.userName ?? u.email ?? '';
    return name.charAt(0).toUpperCase();
  });

  readonly countryName = computed(() => {
    const c = this.country();
    if (!c) return '—';
    return this.localeService.locale() === 'ar' ? c.nameAr : c.nameEn;
  });

  knowledgeLevelKey(level: string): string {
    return `users.detail.knowledgeLevel.${level.toLowerCase()}`;
  }

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) { this.error.set({ kind: 'not-found' }); return; }
    await this.load(id);
  }

  async load(id: string): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    const res = await this.api.getUser(id);
    if (res.ok) {
      this.user.set(res.value);
      if (res.value.countryId) {
        const cRes = await this.countryApi.getCountry(res.value.countryId);
        if (cRes.ok) this.country.set(cRes.value);
      }
    } else {
      this.error.set(res.error);
    }
    this.loading.set(false);
  }

  async deleteUser(): Promise<void> {
    const u = this.user();
    if (!u) return;
    const confirmed = await this.confirm.confirm({
      titleKey: 'users.delete.confirmTitle',
      messageKey: 'users.delete.confirmMessage',
      confirmKey: 'users.delete.confirmButton',
      cancelKey: 'common.actions.cancel',
    });
    if (!confirmed) return;
    const res = await this.api.deleteUser(u.id);
    if (res.ok) {
      this.toast.success('users.delete.successToast');
      void this.router.navigate(['/users']);
    } else {
      this.toast.error('users.delete.errorGeneric');
    }
  }

}
