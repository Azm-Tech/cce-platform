import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { TranslateModule } from '@ngx-translate/core';
import { AssistantDialogComponent } from '../../features/assistant/assistant.dialog';
import { HeaderComponent } from './header.component';
import { FooterComponent } from './footer.component';

/**
 * Top-level shell for the public web-portal. Hosts the sticky header,
 * routed content, footer, and a floating Assistant FAB anchored to
 * the bottom-leading corner of the viewport.
 *
 * Clicking the FAB opens `AssistantDialogComponent` as a modal dialog
 * sized to fit the bottom-left of the screen (~400 × 640) — a
 * conversational drawer the user can pop open without leaving the
 * current page.
 */
@Component({
  selector: 'cce-portal-shell',
  standalone: true,
  imports: [
    RouterOutlet,
    HeaderComponent,
    FooterComponent,
    MatIconModule,
    TranslateModule,
  ],
  templateUrl: './portal-shell.component.html',
  styleUrl: './portal-shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PortalShellComponent {
  private readonly dialog = inject(MatDialog);

  openAssistant(): void {
    // Side sheet — anchored to the trailing edge of the viewport,
    // full height, ~460px wide. The actual position is set globally
    // via the `cce-assistant-dialog-panel` class in styles.scss
    // (cdk-overlay renders outside any component scope).
    this.dialog.open(AssistantDialogComponent, {
      width: '460px',
      height: '100vh',
      maxWidth: '100vw',
      maxHeight: '100vh',
      panelClass: 'cce-assistant-dialog-panel',
      hasBackdrop: true,
      backdropClass: 'cce-assistant-dialog-backdrop',
      autoFocus: 'first-tabbable',
      restoreFocus: true,
      enterAnimationDuration: '320ms',
      exitAnimationDuration: '220ms',
    });
  }
}
