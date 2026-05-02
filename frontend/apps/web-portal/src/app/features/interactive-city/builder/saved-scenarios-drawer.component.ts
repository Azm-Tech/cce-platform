import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * Stub for Phase 04 — auth-only side rail (desktop) / bottom sheet (mobile).
 */
@Component({
  selector: 'cce-saved-scenarios-drawer',
  standalone: true,
  imports: [],
  templateUrl: './saved-scenarios-drawer.component.html',
  styleUrl: './saved-scenarios-drawer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SavedScenariosDrawerComponent {}
