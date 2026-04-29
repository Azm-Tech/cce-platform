import { Directive, EmbeddedViewRef, Input, TemplateRef, ViewContainerRef, effect, inject } from '@angular/core';
import { AuthService } from './auth.service';

@Directive({
  selector: '[ccePermission]',
  standalone: true,
})
export class PermissionDirective {
  private readonly auth = inject(AuthService);
  private readonly tpl = inject(TemplateRef<unknown>);
  private readonly vcr = inject(ViewContainerRef);
  private viewRef: EmbeddedViewRef<unknown> | null = null;
  private requiredPermission: string | null = null;

  constructor() {
    effect(() => {
      // Re-evaluate whenever currentUser changes.
      this.auth.currentUser();
      this.update();
    });
  }

  @Input({ required: true }) set ccePermission(value: string) {
    this.requiredPermission = value;
    this.update();
  }

  private update(): void {
    const allowed = this.requiredPermission !== null && this.auth.hasPermission(this.requiredPermission);
    if (allowed && !this.viewRef) {
      this.viewRef = this.vcr.createEmbeddedView(this.tpl);
    } else if (!allowed && this.viewRef) {
      this.vcr.clear();
      this.viewRef = null;
    }
  }
}
