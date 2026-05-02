import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import type { ViewerTab } from './map-viewer-store.service';

/**
 * Horizontal tab strip showing every open map. Click a tab to switch
 * active; click the × to close. The active tab gets a brand-blue
 * underline. Strip scrolls horizontally on overflow (mobile-friendly).
 *
 * Renders nothing when the tabs list is empty.
 */
@Component({
  selector: 'cce-tabs-bar',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, TranslateModule],
  templateUrl: './tabs-bar.component.html',
  styleUrl: './tabs-bar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TabsBarComponent {
  readonly tabs = input.required<ViewerTab[]>();
  readonly activeId = input<string | null>(null);
  readonly locale = input<'ar' | 'en'>('en');

  readonly tabSelected = output<string>();
  readonly tabClosed = output<string>();

  readonly hasTabs = computed(() => this.tabs().length > 0);

  /** Resolves a tab's localized label. */
  labelOf(tab: ViewerTab): string {
    return this.locale() === 'ar' ? tab.metadata.nameAr : tab.metadata.nameEn;
  }

  isActive(tab: ViewerTab): boolean {
    return this.activeId() === tab.id;
  }

  onSelect(tab: ViewerTab): void {
    if (this.isActive(tab)) return;
    this.tabSelected.emit(tab.id);
  }

  onClose(tab: ViewerTab, event: Event): void {
    // Prevent the click from bubbling to the tab button (which would
    // emit tabSelected before the closed handler fires).
    event.stopPropagation();
    this.tabClosed.emit(tab.id);
  }
}
