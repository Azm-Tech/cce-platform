import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatTabsModule } from '@angular/material/tabs';
import { TranslocoModule } from '@jsverse/transloco';
import { HomepageSectionsPage } from './homepage-sections.page';
import { HomepageSettingsPage } from './homepage-settings.page';

@Component({
  selector: 'cce-homepage',
  standalone: true,
  imports: [MatTabsModule, TranslocoModule, HomepageSectionsPage, HomepageSettingsPage],
  templateUrl: './homepage.page.html',
  styleUrl: './homepage.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomepagePage {}
