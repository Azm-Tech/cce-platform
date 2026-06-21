import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { FollowDirective } from '../follows/follow.directive';
import { AuthService } from '../../core/auth/auth.service';
import { CommunityApiService } from './community-api.service';
import type { CommunityUserProfile } from './community.types';

@Component({
  selector: 'cce-community-user-profile-page',
  standalone: true,
  imports: [MatIconModule, TranslocoModule, FollowDirective],
  templateUrl: './community-user-profile.page.html',
  styleUrl: './community-user-profile.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CommunityUserProfilePage implements OnInit {
  private readonly api = inject(CommunityApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(AuthService);
  private readonly localeService = inject(LocaleService);

  readonly profile = signal<CommunityUserProfile | null>(null);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly isAuthenticated = this.auth.isAuthenticated;
  readonly locale = this.localeService.locale;

  readonly userId = computed(() => this.route.snapshot.paramMap.get('id'));
  readonly currentUserId = computed(() => this.auth.currentUser()?.id ?? null);
  readonly isOwnProfile = computed(() => !!this.currentUserId() && this.currentUserId() === this.userId());

  readonly displayName = computed(() => {
    const p = this.profile();
    if (!p) return '';
    return [p.firstName, p.lastName].filter(Boolean).join(' ');
  });

  readonly initial = computed(() =>
    this.displayName().charAt(0).toUpperCase() || '؟',
  );

  readonly joinedOnFormatted = computed(() => {
    const d = this.profile()?.joinedOn;
    if (!d) return null;
    try {
      const loc = this.locale() === 'ar' ? 'ar-SA' : 'en-US';
      return new Intl.DateTimeFormat(loc, { month: 'long', year: 'numeric' }).format(new Date(d));
    } catch {
      return null;
    }
  });

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    const id = this.userId();
    if (!id) return;
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.getCommunityUser(id);
    this.loading.set(false);
    if (res.ok) {
      this.profile.set(res.value);
    } else {
      this.errorKind.set(res.error.kind);
    }
  }
}
