import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoModule } from '@jsverse/transloco';
import { LocaleService } from '@frontend/i18n';
import { FollowDirective } from '../follows/follow.directive';
import { FollowsApiService } from '../follows/follows-api.service';
import { CommunityApiService } from './community-api.service';

interface FollowedUser {
  userId: string;
  name: string;
  initial: string;
  isExpert: boolean;
}

@Component({
  selector: 'cce-community-following-dialog',
  standalone: true,
  imports: [MatDialogModule, MatIconModule, TranslocoModule, FollowDirective],
  // Default required — dialog overlay boundary breaks Transloco with OnPush
  changeDetection: ChangeDetectionStrategy.Default,
  template: `
    <div class="cfd__header">
      <h2 class="cfd__title">{{ 'community.following.title' | transloco }}</h2>
      <button type="button" class="cfd__close" (click)="close()" [attr.aria-label]="'community.following.close' | transloco">
        <mat-icon svgIcon="x" aria-hidden="true"></mat-icon>
      </button>
    </div>

    <div class="cfd__body">
      @if (loading()) {
        @for (s of skeletons; track $index) {
          <div class="cfd__row cfd__row--skel" aria-hidden="true">
            <span class="cfd__avatar"></span>
            <span class="cfd__skel-name"></span>
          </div>
        }
      } @else if (errorKind()) {
        <div class="cfd__error" role="alert">
          <mat-icon svgIcon="circle-x" aria-hidden="true"></mat-icon>
          <span>{{ ('errors.' + errorKind()) | transloco }}</span>
          <button type="button" (click)="load()">{{ 'errors.retry' | transloco }}</button>
        </div>
      } @else if (users().length === 0) {
        <div class="cfd__empty">
          <mat-icon svgIcon="users" aria-hidden="true"></mat-icon>
          <p>{{ 'community.following.empty' | transloco }}</p>
        </div>
      } @else {
        @for (u of users(); track u.userId) {
          <div class="cfd__row">
            <span class="cfd__avatar" aria-hidden="true">{{ u.initial }}</span>
            <span class="cfd__name">
              {{ u.name }}
              @if (u.isExpert) {
                <mat-icon class="cfd__expert" svgIcon="badge-check" aria-hidden="true"></mat-icon>
              }
            </span>
            <button
              type="button"
              class="cfd__follow"
              [class.cfd__follow--active]="userFollow.isFollowing()"
              cceFollow
              [entityType]="'user'"
              [entityId]="u.userId"
              #userFollow="cceFollow"
              [attr.aria-label]="(userFollow.isFollowing() ? 'community.detail.followingAuthor' : 'community.detail.followAuthor') | transloco"
            >
              <mat-icon svgIcon="{{ userFollow.isFollowing() ? 'bookmark-check' : 'bookmark' }}" aria-hidden="true"></mat-icon>
            </button>
          </div>
        }
      }
    </div>
  `,
  styleUrl: './community-following-dialog.component.scss',
})
export class CommunityFollowingDialogComponent implements OnInit {
  private readonly api = inject(CommunityApiService);
  private readonly followsApi = inject(FollowsApiService);
  private readonly localeService = inject(LocaleService);
  private readonly dialogRef = inject<MatDialogRef<CommunityFollowingDialogComponent>>(MatDialogRef);

  readonly locale = this.localeService.locale;
  readonly users = signal<FollowedUser[]>([]);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);
  readonly skeletons = Array.from({ length: 4 });

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const followsRes = await this.followsApi.getMyFollows();
    if (!followsRes.ok) {
      this.loading.set(false);
      this.errorKind.set(followsRes.error.kind);
      return;
    }
    const ids = followsRes.value.userIds;
    if (ids.length === 0) {
      this.users.set([]);
      this.loading.set(false);
      return;
    }
    const profiles = await Promise.all(ids.map((id) => this.api.getCommunityUser(id)));
    this.loading.set(false);
    const list: FollowedUser[] = [];
    for (const res of profiles) {
      if (!res.ok) continue;
      const p = res.value;
      const name = [p.firstName, p.lastName].filter(Boolean).join(' ').trim();
      list.push({
        userId: p.userId,
        name: name || (this.locale() === 'ar' ? 'عضو' : 'Member'),
        initial: (name.charAt(0) || '؟').toUpperCase(),
        isExpert: p.isExpert,
      });
    }
    this.users.set(list);
  }

  close(): void {
    this.dialogRef.close();
  }

  /** Open the dialog with consistent config from any caller. */
  static open(dialog: MatDialog): MatDialogRef<CommunityFollowingDialogComponent> {
    return dialog.open(CommunityFollowingDialogComponent, {
      width: '560px',
      maxWidth: '95vw',
      maxHeight: '85vh',
      autoFocus: false,
      panelClass: 'cce-dialog-no-padding',
    });
  }
}
