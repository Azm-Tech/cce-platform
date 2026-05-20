import {
  Directive, EmbeddedViewRef, Input, OnChanges,
  TemplateRef, ViewContainerRef, effect, inject,
} from '@angular/core';
import { CcePortalRole } from '@frontend/contracts';
import { AuthService } from './auth.service';

/**
 * Renders the embedded template only when the signed-in user has the
 * specified portal role. Reacts to sign-in / sign-out automatically.
 *
 * Usage:
 *   <button *cceIfRole="CcePortalRole.Expert">Mark as answer</button>
 *
 * Multiple roles — show if user has ANY of them:
 *   <div *cceIfRole="[CcePortalRole.Expert, CcePortalRole.User]">...</div>
 *
 * With else:
 *   <div *cceIfRole="CcePortalRole.Expert; else notExpert">Expert UI</div>
 *   <ng-template #notExpert>Regular user</ng-template>
 */
@Directive({
  selector: '[cceIfRole]',
  standalone: true,
})
export class IfRoleDirective implements OnChanges {
  private readonly auth = inject(AuthService);
  private readonly tpl = inject(TemplateRef<unknown>);
  private readonly vcr = inject(ViewContainerRef);
  private viewRef: EmbeddedViewRef<unknown> | null = null;
  private elseViewRef: EmbeddedViewRef<unknown> | null = null;
  private elseTpl: TemplateRef<unknown> | null = null;

  @Input() cceIfRole: CcePortalRole | CcePortalRole[] = [];
  @Input() set cceIfRoleElse(tpl: TemplateRef<unknown> | null) {
    this.elseTpl = tpl;
    this.update();
  }

  constructor() {
    effect(() => {
      void this.auth.roles(); // track signal
      this.update();
    });
  }

  ngOnChanges(): void {
    this.update();
  }

  private update(): void {
    const roles = Array.isArray(this.cceIfRole) ? this.cceIfRole : [this.cceIfRole];
    const allowed = roles.some((r) => this.auth.hasRole(r));

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
