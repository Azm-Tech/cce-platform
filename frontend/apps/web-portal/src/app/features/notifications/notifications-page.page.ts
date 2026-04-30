import { ChangeDetectionStrategy, Component, OnInit, ViewChild, inject } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { NotificationsDrawerComponent } from './notifications-drawer.component';

/**
 * Full-page wrapper around NotificationsDrawerComponent. The drawer
 * content is identical in both contexts (drawer + page); this page
 * just gives the user a permanent URL at /me/notifications and a
 * wider page chrome.
 */
@Component({
  selector: 'cce-notifications-page',
  standalone: true,
  imports: [TranslateModule, NotificationsDrawerComponent],
  template: `
    <section class="cce-notifications-page">
      <cce-notifications-drawer #drawer />
    </section>
  `,
  styles: [
    `:host { display: block; padding: 1.5rem; max-width: 720px; margin: 0 auto; }
     cce-notifications-drawer { display: block; min-width: 0; max-width: none; }`,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationsPage implements OnInit {
  @ViewChild('drawer', { static: true }) drawer!: NotificationsDrawerComponent;

  ngOnInit(): void {
    void this.drawer.refresh();
  }
}
