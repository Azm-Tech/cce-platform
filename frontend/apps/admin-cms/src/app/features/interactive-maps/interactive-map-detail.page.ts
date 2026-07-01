import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslocoModule } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';
import { LocaleService } from '@frontend/i18n';
import { ConfirmDialogService, ToastService, iconDataUri, isCustomIconUrl } from '@frontend/ui-kit';
import { PermissionDirective } from '../../core/auth/permission.directive';
import { InteractiveMapsApiService } from './interactive-maps-api.service';
import { InteractiveMapFormDialogComponent } from './interactive-map-form.dialog';
import { InteractiveMapNodeFormDialogComponent } from './interactive-map-node-form.dialog';
import type { InteractiveMapDto, InteractiveMapNodeDto } from './interactive-maps.types';


/**
 * Editor for the single Interactive Map the system owns.
 *
 * There is no map list and no map creation/deletion — the map is seeded
 * server-side and resolved here via `getCurrentMap()`. This page edits the
 * map's metadata ("Edit details") and performs full CRUD on its nodes.
 */
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
  private readonly localeService = inject(LocaleService);

  /** Active UI language — drives which localized name/content is primary. */
  readonly locale = this.localeService.locale;

  readonly mapId = signal('');
  readonly map = signal<InteractiveMapDto | null>(null);
  readonly nodes = signal<InteractiveMapNodeDto[]>([]);
  readonly loading = signal(false);
  readonly errorKind = signal<string | null>(null);

  readonly treeNodes = computed(() => this.buildTreeOrder(this.nodes()));

  // ── Collapse / expand state ──────────────────────────────────────────────
  /** IDs of level-0 roots that are currently expanded; all others are collapsed. */
  readonly expandedRoots = signal<Set<string>>(new Set());

  /** Set of node IDs that have at least one direct child. */
  readonly parentIds = computed(() => {
    const set = new Set<string>();
    for (const n of this.nodes()) {
      if (n.parentId) set.add(n.parentId);
    }
    return set;
  });

  /** Direct child count per node ID (for the collapsed-badge). */
  readonly directChildCounts = computed(() => {
    const counts = new Map<string, number>();
    for (const n of this.nodes()) {
      if (n.parentId) counts.set(n.parentId, (counts.get(n.parentId) ?? 0) + 1);
    }
    return counts;
  });

  readonly visibleNodes = computed(() => {
    const all = this.treeNodes();
    const expanded = this.expandedRoots();
    // Fast path: nothing expanded — show root nodes only (parentId === null).
    if (expanded.size === 0) return all.filter((n) => n.parentId === null);
    const parentMap = new Map(all.map((n) => [n.id, n.parentId]));
    return all.filter((node) => {
      if (node.parentId === null) return true; // root nodes always show
      let current: string | null = node.parentId;
      while (current != null) {
        const grandParent = parentMap.get(current);
        if (grandParent === null) return expanded.has(current); // current is a root
        if (grandParent === undefined) return true;             // orphan reference
        current = grandParent;
      }
      return true;
    });
  });

  readonly hasCollapsibleRoots = computed(() =>
    this.treeNodes().some((n) => n.parentId === null && this.parentIds().has(n.id)),
  );

  readonly allExpanded = computed(() => {
    const collapsible = this.treeNodes().filter((n) => n.parentId === null && this.parentIds().has(n.id));
    return collapsible.length > 0 && collapsible.every((n) => this.expandedRoots().has(n.id));
  });

  isExpanded(id: string): boolean { return this.expandedRoots().has(id); }
  isParent(id: string): boolean { return this.parentIds().has(id); }
  childCount(id: string): number { return this.directChildCounts().get(id) ?? 0; }

  toggleCollapse(id: string): void {
    this.expandedRoots.update((set) => {
      const next = new Set(set);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  }

  toggleAll(): void {
    const roots = this.treeNodes()
      .filter((n) => n.parentId === null && this.parentIds().has(n.id))
      .map((n) => n.id);
    this.expandedRoots.set(this.allExpanded() ? new Set() : new Set(roots));
  }

  // ── Localized display: primary follows the selected language, falling back
  //    to the other language; `*Alt` returns the other language (shown small). ──
  private primary(ar: string | null, en: string | null): string {
    return (this.locale() === 'ar' ? ar || en : en || ar) ?? '';
  }
  private alt(ar: string | null, en: string | null): string {
    return (this.locale() === 'ar' ? en : ar) ?? '';
  }
  /** Direction for the small secondary (alt-language) line. */
  readonly altDir = computed<'rtl' | 'ltr'>(() => (this.locale() === 'ar' ? 'ltr' : 'rtl'));

  mapTitle(m: InteractiveMapDto): string { return this.primary(m.nameAr, m.nameEn) || '—'; }
  mapTitleAlt(m: InteractiveMapDto): string { return this.alt(m.nameAr, m.nameEn); }
  mapDesc(m: InteractiveMapDto): string { return this.primary(m.descriptionAr, m.descriptionEn); }

  nodeName(n: InteractiveMapNodeDto): string { return this.primary(n.nameAr, n.nameEn) || '—'; }
  nodeNameAlt(n: InteractiveMapNodeDto): string { return this.alt(n.nameAr, n.nameEn); }

  // ── Node icon preview (registry key or uploaded URL) ──
  nodeIcon(n: InteractiveMapNodeDto): string | null {
    return n.iconKey ? iconDataUri(n.iconKey) : null;
  }
  nodeIconLabel(n: InteractiveMapNodeDto): string {
    return n.iconKey && !isCustomIconUrl(n.iconKey) ? n.iconKey : '';
  }

  ngOnInit(): void {
    void this.reload();
  }

  /** Resolve the single map, then load its nodes. */
  async reload(): Promise<void> {
    this.loading.set(true);
    this.errorKind.set(null);
    const res = await this.api.getCurrentMap();
    if (!res.ok) {
      this.loading.set(false);
      this.errorKind.set(res.error.kind);
      return;
    }
    this.map.set(res.value);
    this.mapId.set(res.value.id);
    await this.loadNodes();
  }

  async loadNodes(): Promise<void> {
    if (!this.mapId()) return;
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

  /** Edit the single map's metadata (name/description/active) — never creates. */
  async editDetails(): Promise<void> {
    const current = this.map();
    if (!current) return;
    const ref = this.dialog.open(InteractiveMapFormDialogComponent, {
      data: { map: current },
      width: '640px',
    });
    if (await firstValueFrom(ref.afterClosed())) {
      this.toast.success('interactiveMaps.toast.updated');
      const res = await this.api.getCurrentMap();
      if (res.ok) this.map.set(res.value);
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
