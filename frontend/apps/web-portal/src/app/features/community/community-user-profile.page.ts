import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoModule } from '@jsverse/transloco';
import { FollowDirective } from '../follows/follow.directive';
import { AuthService } from '../../core/auth/auth.service';
import { CommunityApiService } from './community-api.service';
import type { CommunityUserProfile } from './community.types';

@Component({
  selector: 'cce-community-user-profile-page',
  standalone: true,
  imports: [RouterLink, MatIconModule, TranslocoModule, FollowDirective],
  template: `
    <div class="cup-page">

      @if (loading()) {
        <div class="cup-skeleton" aria-busy="true">
          <div class="cup-skel cup-skel--avatar"></div>
          <div class="cup-skel cup-skel--name"></div>
          <div class="cup-skel cup-skel--line"></div>
          <div class="cup-skel cup-skel--stats"></div>
        </div>
      }

      @if (errorKind()) {
        <div class="cup-error" role="alert">
          <mat-icon svgIcon="circle-x" aria-hidden="true"></mat-icon>
          <span>{{ ('errors.' + errorKind()) | transloco }}</span>
          <button type="button" (click)="load()">{{ 'errors.retry' | transloco }}</button>
        </div>
      }

      @if (profile(); as prof) {
        <div class="cup-card">
          <div class="cup-card__avatar" aria-hidden="true">{{ initial() }}</div>

          <div class="cup-card__info">
            <h1 class="cup-card__name">{{ displayName() }}</h1>
            @if (prof.isExpert) {
              <span class="cup-card__expert">
                <mat-icon svgIcon="badge-check" aria-hidden="true"></mat-icon>
                {{ 'community.profile.expertBadge' | transloco }}
              </span>
            }
            @if (prof.jobTitle) {
              <span class="cup-card__job">{{ prof.jobTitle }}</span>
            }
            @if (prof.organizationName) {
              <span class="cup-card__org">{{ prof.organizationName }}</span>
            }
          </div>

          <div class="cup-card__stats">
            <div class="cup-card__stat">
              <strong>{{ prof.postCount }}</strong>
              <span>{{ 'community.profile.posts' | transloco }}</span>
            </div>
            <div class="cup-card__stat-divider"></div>
            <div class="cup-card__stat">
              <strong>{{ prof.replyCount }}</strong>
              <span>{{ 'community.profile.replies' | transloco }}</span>
            </div>
          </div>

          @if (isAuthenticated() && userId()) {
            <button
              type="button"
              class="cup-card__follow-btn"
              [class.cup-card__follow-btn--active]="followRef.isFollowing()"
              cceFollow
              entityType="user"
              [entityId]="userId()!"
              #followRef="cceFollow"
            >
              <mat-icon [svgIcon]="followRef.isFollowing() ? 'bookmark-check' : 'bookmark'" aria-hidden="true"></mat-icon>
              {{ (followRef.isFollowing() ? 'community.detail.followingAuthor' : 'community.detail.followAuthor') | transloco }}
            </button>
          }

          <a routerLink="/community" class="cup-card__back">
            <mat-icon svgIcon="chevron-right" aria-hidden="true"></mat-icon>
            {{ 'community.detail.backToCommunity' | transloco }}
          </a>
        </div>
      }

    </div>
  `,
  styleUrl: './community-user-profile.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CommunityUserProfilePage implements OnInit {
  private readonly api = inject(CommunityApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(AuthService);

  readonly profile = signal<CommunityUserProfile | null>(null);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly isAuthenticated = this.auth.isAuthenticated;

  readonly userId = computed(() => this.route.snapshot.paramMap.get('id'));

  readonly displayName = computed(() => {
    const p = this.profile();
    if (!p) return '';
    return [p.firstName, p.lastName].filter(Boolean).join(' ');
  });

  readonly initial = computed(() =>
    this.displayName().charAt(0).toUpperCase() || '؟',
  );

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
