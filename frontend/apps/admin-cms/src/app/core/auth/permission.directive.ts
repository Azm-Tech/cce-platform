import {
  Directive, EmbeddedViewRef, Input, TemplateRef, ViewContainerRef, effect, inject,
} from '@angular/core';
import { CcePermission } from '@frontend/contracts';
import { AuthService } from './auth.service';

/**
 * Renders the embedded template only when the signed-in user has the
 * specified permission. Reacts to sign-in / sign-out automatically.
 *
 * Usage:
 *   <button *ccePermission="CcePermission.NewsUpdate">Publish</button>
 *
 * With else:
 *   <div *ccePermission="CcePermission.AuditRead; else noAccess">Audit log</div>
 *   <ng-template #noAccess>Access denied</ng-template>
 */
@Directive({
  selector: '[ccePermission]',
  standalone: true,
})
export class PermissionDirective {
  private readonly auth = inject(AuthService);
  private readonly tpl = inject(TemplateRef<unknown>);
  private readonly vcr = inject(ViewContainerRef);
  private viewRef: EmbeddedViewRef<unknown> | null = null;
  private elseViewRef: EmbeddedViewRef<unknown> | null = null;
  private elseTpl: TemplateRef<unknown> | null = null;
  private requiredPermission: CcePermission | null = null;

  constructor() {
    effect(() => {
      this.auth.currentUser();
      this.update();
    });
  }

  @Input({ required: true }) set ccePermission(value: CcePermission) {
    this.requiredPermission = value;
    this.update();
  }

  @Input() set ccePermissionElse(tpl: TemplateRef<unknown> | null) {
    this.elseTpl = tpl;
    this.update();
  }

  private update(): void {
    const allowed = this.requiredPermission !== null && this.auth.hasPermission(this.requiredPermission);

    if (allowed) {
      if (!this.viewRef) {
        this.vcr.clear();
        this.elseViewRef = null;
        this.viewRef = this.vcr.createEmbeddedView(this.tpl);
      }
    } else {
      if (this.viewRef) {
        this.vcr.clear();
        this.viewRef = null;
      }
      if (this.elseTpl && !this.elseViewRef) {
        this.elseViewRef = this.vcr.createEmbeddedView(this.elseTpl);
      }
    }
  }
}
