import { Directive, EmbeddedViewRef, TemplateRef, ViewContainerRef, effect, inject } from '@angular/core';
import { AuthService } from './auth.service';

/**
 * Renders the embedded template only when the user is signed in.
 * Anonymous-friendly counterpart pattern: pages can use *ifAnonymous below,
 * or render the inline "Sign in to continue" affordance inside an *ngIf.
 */
@Directive({
  selector: '[ifAuthenticated]',
  standalone: true,
})
export class IfAuthenticatedDirective {
  private readonly auth = inject(AuthService);
  private readonly tpl = inject(TemplateRef<unknown>);
  private readonly vcr = inject(ViewContainerRef);
  private viewRef: EmbeddedViewRef<unknown> | null = null;

  constructor() {
    effect(() => {
      const allowed = this.auth.isAuthenticated();
      if (allowed && !this.viewRef) {
        this.viewRef = this.vcr.createEmbeddedView(this.tpl);
      } else if (!allowed && this.viewRef) {
        this.vcr.clear();
        this.viewRef = null;
      }
    });
  }
}
