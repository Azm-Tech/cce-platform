import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { TranslateModule } from '@ngx-translate/core';
import { AuthService } from '../auth/auth.service';
import { LocaleSwitcherComponent } from '../../locale-switcher/locale-switcher.component';
import { SearchBoxComponent } from './search-box.component';
import { PRIMARY_NAV } from './nav-config';

@Component({
  selector: 'cce-header',
  standalone: true,
  imports: [
    CommonModule, RouterLink, RouterLinkActive,
    MatButtonModule, MatIconModule, MatMenuModule,
    TranslateModule, LocaleSwitcherComponent, SearchBoxComponent,
  ],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HeaderComponent {
  private readonly auth = inject(AuthService);
  readonly nav = PRIMARY_NAV;
  readonly mobileMenuOpen = signal(false);
  readonly isAuthenticated = this.auth.isAuthenticated;
  readonly userLabel = computed(() => {
    const u = this.auth.currentUser();
    return u?.displayNameEn ?? u?.userName ?? u?.email ?? '';
  });

  toggleMobileMenu(): void {
    this.mobileMenuOpen.update((v) => !v);
  }
  signIn(): void { this.auth.signIn(); }
  async signOut(): Promise<void> { await this.auth.signOut(); }
}
