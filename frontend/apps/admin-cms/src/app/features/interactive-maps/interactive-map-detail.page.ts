import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslocoModule } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';
import { ConfirmDialogService, ToastService } from '@frontend/ui-kit';
import { PermissionDirective } from '../../core/auth/permission.directive';
import { InteractiveMapsApiService } from './interactive-maps-api.service';
import { InteractiveMapNodeFormDialogComponent } from './interactive-map-node-form.dialog';
import type { InteractiveMapDto, InteractiveMapNodeDto } from './interactive-maps.types';

@Component({
  selector: 'cce-interactive-map-detail',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatTooltipModule,
    TranslocoModule,
    PermissionDirective,
  ],
  templateUrl: './interactive-map-detail.page.html',
  styleUrl: './interactive-map-detail.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InteractiveMapDetailPage implements OnInit {
  private readonly api = inject(InteractiveMapsApiService);
  private readonly dialog = inject(MatDialog);
  private readonly confirm = inject(ConfirmDialogService);
  private readonly toast = inject(ToastService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly mapId = signal('');
  readonly map = signal<InteractiveMapDto | null>(null);
  readonly nodes = signal<InteractiveMapNodeDto[]>([]);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  readonly treeNodes = computed(() => this.buildTreeOrder(this.nodes()));

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.mapId.set(id);
    void this.loadMap(id);
    void this.loadNodes();
  }

  private async loadMap(id: string): Promise<void> {
    const res = await this.api.getMap(id);
    if (res.ok) this.map.set(res.value);
  }

  async loadNodes(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.listNodes(this.mapId(), { page: 1, pageSize: 200 });
    this.loading.set(false);
    if (res.ok) {
      this.nodes.set(res.value.items);
    } else {
      this.errorKind.set(res.error.kind);
    }
  }

  async openCreate(): Promise<void> {
    const ref = this.dialog.open(InteractiveMapNodeFormDialogComponent, {
      data: { mapId: this.mapId(), existingNodes: this.nodes() },
      width: '720px',
    });
    if (await firstValueFrom(ref.afterClosed())) {
      this.toast.success('interactiveMaps.nodes.toast.created');
      void this.loadNodes();
    }
  }

  async openEdit(node: InteractiveMapNodeDto): Promise<void> {
    const ref = this.dialog.open(InteractiveMapNodeFormDialogComponent, {
      data: { mapId: this.mapId(), node, existingNodes: this.nodes() },
      width: '720px',
    });
    if (await firstValueFrom(ref.afterClosed())) {
      this.toast.success('interactiveMaps.nodes.toast.updated');
      void this.loadNodes();
    }
  }

  async delete(node: InteractiveMapNodeDto): Promise<void> {
    if (
      !(await this.confirm.confirm({
        titleKey: 'interactiveMaps.nodes.delete.title',
        messageKey: 'interactiveMaps.nodes.delete.message',
        confirmKey: 'interactiveMaps.nodes.delete.confirm',
        cancelKey: 'common.actions.cancel',
      }))
    ) return;
    const res = await this.api.deleteNode(this.mapId(), node.id);
    if (res.ok) {
      this.toast.success('interactiveMaps.nodes.toast.deleted');
      void this.loadNodes();
    } else {
      this.toast.error(`errors.${res.error.kind}`);
    }
  }

  goBack(): void {
    void this.router.navigate(['/interactive-maps']);
  }

  private buildTreeOrder(nodes: InteractiveMapNodeDto[]): InteractiveMapNodeDto[] {
    const byParent = new Map<string | null, InteractiveMapNodeDto[]>();
    for (const node of nodes) {
      const key = node.parentId ?? null;
      const bucket = byParent.get(key) ?? [];
      bucket.push(node);
      byParent.set(key, bucket);
    }
    const result: InteractiveMapNodeDto[] = [];
    const walk = (parentId: string | null): void => {
      const children = byParent.get(parentId) ?? [];
      for (const child of children) {
        result.push(child);
        walk(child.id);
      }
    };
    walk(null);
    // Append orphans (parentId references a deleted or missing node)
    for (const node of nodes) {
      if (!result.includes(node)) result.push(node);
    }
    return result;
  }
}
