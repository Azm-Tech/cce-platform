import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * Stub for Phase 03 — sticky bottom bar with live totals + Run + Save buttons.
 */
@Component({
  selector: 'cce-totals-bar',
  standalone: true,
  imports: [],
  templateUrl: './totals-bar.component.html',
  styleUrl: './totals-bar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TotalsBarComponent {}
