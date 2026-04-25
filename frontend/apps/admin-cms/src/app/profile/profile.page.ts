import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { TranslateModule } from '@ngx-translate/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { map } from 'rxjs';

@Component({
  selector: 'cce-profile-page',
  standalone: true,
  imports: [CommonModule, MatCardModule, TranslateModule],
  templateUrl: './profile.page.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfilePage {
  private readonly oidc = inject(OidcSecurityService);

  readonly userData = toSignal(
    this.oidc.userData$.pipe(map((u) => u?.userData ?? null)),
    { initialValue: null },
  );

  readonly preferredUsername = computed(() => this.userData()?.preferred_username ?? '');
  readonly email = computed(() => this.userData()?.email ?? '');
  readonly upn = computed(() => this.userData()?.upn ?? '');
  readonly groups = computed<string[]>(() => {
    const g = this.userData()?.groups;
    return Array.isArray(g) ? g : [];
  });
}
