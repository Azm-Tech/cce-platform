import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'cce-filter-rail',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, TranslateModule],
  template: `
    <button type="button" mat-button class="cce-filter-rail__toggle" (click)="toggle()">
      <mat-icon>filter_list</mat-icon>
      {{ 'filter.openButton' | translate }}
    </button>
    <aside class="cce-filter-rail" [class.cce-filter-rail--open]="open()">
      <h2 class="cce-filter-rail__title">{{ 'filter.title' | translate }}</h2>
      <ng-content />
    </aside>
  `,
  styleUrl: './filter-rail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FilterRailComponent {
  readonly open = signal(typeof window !== 'undefined' && window.innerWidth > 768);
  toggle(): void { this.open.update((v) => !v); }
}
